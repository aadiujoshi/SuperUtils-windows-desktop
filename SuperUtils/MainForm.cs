using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SuperUtils.Properties;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace SuperUtils
{
    public partial class MainForm : Form
    {
        private NotifyIcon trayIcon;
        private SuperParcel selectedDataParcel;
        private SuperParcel clipboardDataParcel;

        public MainForm()
        {
            selectedDataParcel = new SuperParcel();
            clipboardDataParcel = new SuperParcel();

            InitializeComponent();

            HotkeyManager.Init(this);

            // Set the form size
            //this.ClientSize = new Size(600, 400);
            this.Text = "SuperUtils";

            // Initialize the tray icon

            //Icon appIcon = new Icon("icon.png");

            Icon appIcon;
            using (var stream = new MemoryStream(Properties.Resources.icon))
            {
                appIcon = this.Icon = new Icon(stream);
            }

            trayIcon = new NotifyIcon
            {
                //Icon = SystemIcons.WinLogo, // Use a default system icon
                Icon = appIcon,
                Text = "SuperUtils",
                Visible = true
            };

            // Add a context menu to the tray icon
            var trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Show", null, ShowApp);
            trayMenu.Items.Add("Exit", null, ExitApp);
            trayIcon.ContextMenuStrip = trayMenu;

            // Handle double-click to show the app
            trayIcon.Click += ShowApp;

            HotkeyManager.RegisterHotkey(Keys.Q, HotkeyManager.MOD_CONTROL | HotkeyManager.MOD_SHIFT, HotkeyManager.TOGGLE_SHOW_HK, ToggleApp);
            //HotkeyManager.RegisterHotkey(Keys.S, HotkeyManager.MOD_CONTROL, OPEN_BLUETOOTH_SHARE_HK, () => RunScript("open", "fsquirt.exe"));
        }

        private void ToggleApp()
        {
            if (this.Visible)
            {
                this.Close();
            }
            else
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void ShowApp(object? sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void ExitApp(object? sender, EventArgs e)
        {
            trayIcon.Visible = false;
            TcpConnectionService.Instance.Dispose();
            DebugConsole.Instance.Close();
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // Minimize to tray instead of closing
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            InitNetworkLables();
            LoadClipboard();
            PreviewSelectedFiles();
            
            using (var ms = new MemoryStream(Properties.Resources.drag_and_drop))
            {
                selectDataButton.BackgroundImage = new Bitmap(ms);
            }
        }

        //---------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------
        //
        //                             MANANGE INCOMING USER SELECTED DATA
        //
        //---------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------

        private void selectDataButton_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data == null)
                return;

            // List all available data formats
            string[] formats = e.Data.GetFormats();
            Debug.WriteLine("Available Data Formats:");
            foreach (string format in formats)
            {
                Debug.WriteLine(" - " + format);
            }

            // Handle dropped files
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                Debug.WriteLine("Dropped Files:");
                foreach (string file in files)
                {
                    Debug.WriteLine(" - " + file);
                    string mime = MimeHelper.GetMimeType(file);
                    selectedDataParcel.AddItem(mime, file);
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.Text))
            {
                string text = (string)e.Data.GetData(DataFormats.Text);
                Debug.WriteLine("Dropped Text: " + text);
                selectedDataParcel.AddItem("text/plain", text);
            }
            else
            {
                Debug.WriteLine("Dropped data is not a file or plain text.");
            }
        }

        private void selectDataButton_Click(object sender, EventArgs e)
        {
            var fileList = Util.SelectFilesWithDialog();

            foreach (var file in fileList)
            {
                string mimeType = MimeHelper.GetMimeType(file.FullName);
                DebugConsole.Instance.WriteLine(file.FullName + "  " + file.Name + "  " + mimeType);
                selectedDataParcel.AddItem(mimeType, file);
            }

            PreviewSelectedFiles();
        }

        //---------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------
        //
        //                             SENDING DATA OVER WIFI
        //
        //---------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------

        private void sendSelectedButton_Click(object sender, EventArgs e)
        {
            TcpConnectionService.Instance
                .SendParcelAsync(selectedDataParcel.GetFullByteParcel())
                .ContinueWith(task =>
                {
                    if (task.IsCompletedSuccessfully && task.Result.IsSuccess)
                    {
                        DebugConsole.Instance.WriteLine("Success Result: " + task.Result.Data);
                    }
                    else if (task.IsCompletedSuccessfully && task.Result.IsFailure)
                    {
                        DebugConsole.Instance.WriteLine("Error Result: " + task.Result.Error);
                    }
                    else if (task.IsFaulted)
                    {
                        DebugConsole.Instance.WriteLine("Task faulted: " + task.Exception?.GetBaseException().Message);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }


        private void sendClipboardButton_Click(object sender, EventArgs e)
        {
            TcpConnectionService.Instance
                .SendParcelAsync(clipboardDataParcel.GetFullByteParcel())
                .ContinueWith(task =>
                {
                    if (task.IsCompletedSuccessfully && task.Result.IsSuccess)
                    {
                        DebugConsole.Instance.WriteLine("Success Result: " + task.Result.Data);
                    }
                    else if (task.IsCompletedSuccessfully && task.Result.IsFailure)
                    {
                        DebugConsole.Instance.WriteLine("Error Result: " + task.Result.Error);
                    }
                    else if (task.IsFaulted)
                    {
                        DebugConsole.Instance.WriteLine("Task faulted: " + task.Exception?.GetBaseException().Message);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext()); ;
        }

        //---------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------
        //
        //                             DYNAMIC UI STUFFFFFFFFFF
        //
        //---------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------
        //---------------------------------------------------------------------------------------

        private void InitNetworkLables()
        {
            connectionStatusLabel.Text = TcpConnectionService.Instance.ConnectionStatus.Desc;
            connectionStatusLabel.ForeColor = TcpConnectionService.Instance.ConnectionStatus.Color;

            parcelStatusLabel.Text = TcpConnectionService.Instance.ParcelStatus.Desc;
            parcelStatusLabel.ForeColor = TcpConnectionService.Instance.ParcelStatus.Color;

            parcelProgressLabel.Text = "0%";
            parcelProgressLabel.ForeColor = Color.Yellow;

            TcpConnectionService.Instance.OnConnectionStatusUpdate += (status) =>
            {
                connectionStatusLabel.SafeInvoke(() =>
                {
                    connectionStatusLabel.Text = status.Desc;
                    connectionStatusLabel.ForeColor = TcpConnectionService.Instance.ConnectionStatus.Color;
                });
            };

            TcpConnectionService.Instance.OnParcelStatusUpdate += (status) =>
            {
                parcelStatusLabel.SafeInvoke(() =>
                {
                    parcelStatusLabel.Text = status.Desc;
                    parcelStatusLabel.ForeColor = TcpConnectionService.Instance.ParcelStatus.Color;
                });
            };

            TcpConnectionService.Instance.OnParcelProgressUpdate += (done, total) =>
            {
                parcelProgressLabel.SafeInvoke(() =>
                {
                    parcelProgressLabel.Text = (Math.Round((1000f * ((float)done) / total)) / 10).ToString() + "%";
                });
            };
        }

        private void refreshClipboardDataButton_Click(object sender, EventArgs e)
        {
            this.LoadClipboard();
        }

        private void forceRefreshConnectionButton_Click(object sender, EventArgs e)
        {
            TcpConnectionService.Instance.DontRevive = false;
            var result = TcpConnectionService.Instance.ForceKillThenRevive();
            if (result.IsSuccess)
            {
                DebugConsole.Instance.WriteLine(result.Data);
            }
            else
            {
                DebugConsole.Instance.WriteLine(result.Error);
            }
        }

        private void retryConnectionButton_Click(object sender, EventArgs e)
        {
            TcpConnectionService.Instance.DontRevive = false;
            Result<string> result = TcpConnectionService.Instance.KillThenRevive();
            if (result.IsSuccess)
            {
                DebugConsole.Instance.WriteLine(result.Data);
            }
            else
            {
                DebugConsole.Instance.WriteLine(result.Error);
            }
        }

        private void openFolderButton_Click(object sender, EventArgs e)
        {
            Util.OpenAppFolderInExplorer();
        }

        private void killConnectionButton_Click(object sender, EventArgs e)
        {
            TcpConnectionService.Instance.KillForNow();
        }
    }
}
