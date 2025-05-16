using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SuperUtils;

namespace SuperUtils
{
    public class StatusHolder
    {
        public bool IsGood { get; }
        public string Desc { get; }
        public Color Color { get; }

        private StatusHolder(bool good, string desc, Color color)
        {
            IsGood = good;
            Desc = desc;
            Color = color;
        }

        public static StatusHolder Good(string description)
        {
            return new StatusHolder(true, description, Color.LawnGreen);
        }

        public static StatusHolder Bad(string description)
        {
            return new StatusHolder(false, description, Color.Firebrick);
        }

        public static StatusHolder Ok(string description)
        {
            return new StatusHolder(false, description, Color.DodgerBlue);
        }
    }

    internal class TcpConnectionService : IDisposable
    {
        public static StatusHolder WAITING_FOR_CONNECTION = StatusHolder.Ok("Trying to connect...");
        public static StatusHolder CONNECTED_TO_PHONE = StatusHolder.Good("Connected to phone");
        public static StatusHolder IDLE_PARCEL_STREAM = StatusHolder.Ok("Idle parcel stream");
        public static StatusHolder NO_PARCEL_STREAM = StatusHolder.Bad("No open parcel stream");
        public static StatusHolder RECEIVING_PARCEL = StatusHolder.Ok("Receiving Parcel...");
        public static StatusHolder SENDING_PARCEL = StatusHolder.Ok("Sending Parcel...");
        public static StatusHolder RECONNECTING = StatusHolder.Ok("Error occurred... Reconnecting");

        public static TcpConnectionService Instance => _instance;

        private static TcpConnectionService _instance = new TcpConnectionService();

        private bool killed = true;
        private bool inTheMiddleOfSomething = false;

        private TcpClient? client;
        private Stream? socketStream;
        private Thread? clientReceiveThread;
        private Thread? clientSendThread;
        private CancellationTokenSource sendTokenSource = new(); 
        private volatile bool isTryingToConnect = false;

        public bool DontRevive {get; set;}

        public event Action<StatusHolder> OnConnectionStatusUpdate = delegate { };
        public event Action<StatusHolder> OnParcelStatusUpdate = delegate { };
        public event Action<int, int> OnParcelProgressUpdate = delegate { };

        private const int ChunkSize = 1024;
        //TAMU IP ADDR
        //private const string DefaultPhoneIP = "10.246.11.82";
        private const string DefaultPhoneIP = "192.168.1.101"; 
        private const int PhonePort = 8888;

        public bool IsConnected => client?.Connected ?? false;

        private StatusHolder _connectionStatus = WAITING_FOR_CONNECTION;

        public StatusHolder ConnectionStatus
        {
            get { return _connectionStatus; }
            private set
            {
                if (_connectionStatus.Desc != value.Desc)
                {
                    _connectionStatus = value;
                    OnConnectionStatusUpdate.Invoke(value);
                }
            }
        }

        public StatusHolder _parcelStatus = NO_PARCEL_STREAM;

        public StatusHolder ParcelStatus
        {
            get { return _parcelStatus; }
            private set
            {
                if (_parcelStatus.Desc != value.Desc)
                {
                    _parcelStatus = value;
                    OnParcelStatusUpdate.Invoke(value);
                }
            }
        }

        public void Init()
        {
            AttemptPhoneConnection();
        }

        private async Task<bool> AttemptPhoneConnection()
        {
            if (DontRevive)
            {
                DebugConsole.Instance.WriteLine("Forced to not revive connect");
                return false;
            }

            if (isTryingToConnect) 
            {
                DebugConsole.Instance.WriteLine("Returned, Already trying to connect");
                return false;
            }
            isTryingToConnect = true;
            inTheMiddleOfSomething = true;

            DebugConsole.Instance.WriteLine("Attempting to connect....");

            // Connect
            client = new TcpClient();
            try
            {
                await client.ConnectAsync(DefaultPhoneIP, PhonePort);
                socketStream = client.GetStream();
                DebugConsole.Instance.WriteLine("Connected to phone");

                ConnectionStatus = CONNECTED_TO_PHONE;
                ParcelStatus = IDLE_PARCEL_STREAM;

                // Start new receive thread
                clientReceiveThread = new Thread(() =>
                {
                    DebugConsole.Instance.WriteLine("Started Receive thread");
                    ReceiveParcelLoop();
                    DebugConsole.Instance.WriteLine("Ended Receive thread");
                });
                clientReceiveThread.Priority = ThreadPriority.BelowNormal;
                clientReceiveThread.IsBackground = true;
                clientReceiveThread.Start();

                killed = false;
                return true;
            }
            catch (Exception ex)
            {
                ConnectionStatus = StatusHolder.Bad($"Failed to connect to {DefaultPhoneIP}:{PhonePort}");
                ParcelStatus = NO_PARCEL_STREAM;
                DebugConsole.Instance.WriteLine($"Specific error: Failed to connect to {DefaultPhoneIP}:{PhonePort}{ex.Message}");
                return false;
            }
            finally
            {
                inTheMiddleOfSomething = false;
                isTryingToConnect = false;
            }
        }

        public async Task<Result<int>> SendParcelAsync(byte[] parcelData)
        {
            if (killed)
            {
                return Result<int>.Failure("Tcp instance is dead");
            }

            if (inTheMiddleOfSomething)
            {
                return Result<int>.Failure("Already Sending or Receiving or Connecting");
            }

            if (SocketIsDead())
            {
                ForceKillThenRevive();
                return Result<int>.Failure("Not connected");
            }

            inTheMiddleOfSomething = true;

            return await Task.Run(async () =>
            {
                var sendToken = sendTokenSource.Token;

                try
                {
                    int total = parcelData.Length;
                    int sent = 0;

                    while (sent < total)
                    {
                        sendToken.ThrowIfCancellationRequested();

                        int chunkLen = Math.Min(ChunkSize, total - sent);
                        await socketStream.WriteAsync(parcelData, sent, chunkLen, sendToken);
                        sent += chunkLen;
                        await socketStream.FlushAsync(sendToken);

                        OnParcelProgressUpdate?.Invoke(sent, total);
                    }

                    return Result<int>.Success(sent);
                }
                catch (OperationCanceledException)
                {
                    return Result<int>.Failure("Send cancelled by user");
                }
                catch (Exception e)
                {
                    ForceKillThenRevive();
                    return Result<int>.Failure("Error occurred while sending: " + e.Message);
                }
                finally
                {
                    inTheMiddleOfSomething = false;
                }
            });
        }

        private void ReceiveParcelLoop()
        {
            if (killed)
            {
                return;
            }

            while (true)
            {
                if (SocketIsDead())
                {
                    DebugConsole.Instance.WriteLine("Socket was dead, refreshing");
                    KillThenRevive();
                    break;
                }

                try
                {
                    byte[] parcelData = ReceiveFullParcel().GetAwaiter().GetResult();
                    if (parcelData != null && parcelData.Length > 0)
                    {
                        inTheMiddleOfSomething = false;
                        HandleReceiveParcelBytes(parcelData);
                    }
                }
                catch (Exception e)
                {
                    DebugConsole.Instance.WriteLine("ReceiveParcelLoop error: " + e.Message);
                    ForceKillThenRevive();
                    return;
                }
            }
        }

        private async Task<byte[]> ReceiveFullParcel()
        {
            var collectedBytes = new MemoryStream();
            byte[] buffer = new byte[ChunkSize];
            int totalReceived = 0;

            var endFlagTracker = new CharSequenceTracker(Encoding.UTF8.GetBytes("END_OF_TRANSMISSION"));

            while (true)
            {
                int bytesRead = await socketStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead <= 0)
                {
                    Thread.Sleep(100);
                    return Array.Empty<byte>();
                }

                collectedBytes.Write(buffer, 0, bytesRead);
                totalReceived += bytesRead;

                OnParcelProgressUpdate?.Invoke(totalReceived, 100000);

                for (int i = 0; i < bytesRead; i++)
                {
                    endFlagTracker.NextChar(buffer[i]);
                    if (endFlagTracker.Found())
                    {
                        return collectedBytes.ToArray(); // found end flag, return collected bytes
                    }
                }
            }
        }

        private void HandleReceiveParcelBytes(byte[] parcelData)
        {
            // Example: parse into SuperParcel
            var result = SuperParcel.FromParcelBytes(parcelData);
            if (result.IsSuccess)
            {
                //OnPar?.Invoke(result.Data);
                //DebugConsole.Instance.WriteLine("Parcel parse Success: ");
                DebugConsole.Instance.WriteLine("Parcel parse Success: " + Encoding.UTF8.GetString(result.Data.GetFullByteParcel()));
                var res = StorageManager.Instance.SaveIncomingSuperParcel(result.Data);
                ClipboardUtils.CopyFilesToClipboard(res);

            }
            else
            {
                DebugConsole.Instance.WriteLine("Parcel parse failed: " + result.Error);
            }
        }

        public Result<string> KillThenRevive()
        {
            if (inTheMiddleOfSomething)
            {
                return Result<string>.Failure("Is in the middle of something");
            }

            Dispose();
            DebugConsole.Instance.WriteLine("Successfully disposed and killed");

            ConnectionStatus = RECONNECTING;
            ParcelStatus = NO_PARCEL_STREAM;
            OnParcelProgressUpdate.Invoke(0, 1);

            AttemptPhoneConnection();

            return Result<string>.Success("Successfully tried to kill and revive");
        }

        public Result<string> ForceKillThenRevive()
        {
            inTheMiddleOfSomething = false;
            isTryingToConnect = false;
            var res = KillThenRevive();
            if (res.IsSuccess)
            {
                DebugConsole.Instance.WriteLine(res.Data);
            }
            else
            {
                DebugConsole.Instance.WriteLine(res.Error);
            }
            return res;
        }

        public void Dispose()
        {
            if (inTheMiddleOfSomething)
            {
                return;
            }
            if (isTryingToConnect)
            {
                return;
            }

            inTheMiddleOfSomething = true;

            killed = true;

            try
            {
                try
                {
                    sendTokenSource.Cancel();
                }
                catch { }

                try
                {
                    if (clientReceiveThread != null && clientReceiveThread.IsAlive)
                    {
                        clientReceiveThread?.Interrupt();
                        DebugConsole.Instance.WriteLine("Ended Receive thread");
                    }
                }
                catch { 

                }
                socketStream?.Close();
                client?.Close();
                DebugConsole.Instance.WriteLine("Closed socket and client");
            }
            catch (Exception e)
            {
                DebugConsole.Instance.WriteLine("Dispose umbrella exception:" + e.Message);
            }
            finally
            {
                sendTokenSource = new CancellationTokenSource();
                clientReceiveThread = null;
                socketStream = null;
                client = null;
                inTheMiddleOfSomething = false;
            }
        }
        
        public bool SocketIsDead()
        {
            return !IsConnected || client == null;
        }
    
        public void KillForNow()
        {

            //hacky code, but i have to do this because it keeeps
            //reviving itself when disposed because of multithreading side effects
            //forces dispose
            DontRevive = true;
            inTheMiddleOfSomething = false;
            Dispose();
        }
    }
}

//try
//{

//}
//catch (ThreadInterruptedException e)
//{
//    DebugConsole.Instance.WriteLine("clientReceiveThread msg:" + e.Message);
//}