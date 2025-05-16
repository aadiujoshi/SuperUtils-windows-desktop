using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SuperUtils
{
    public sealed class DebugConsole
    {
        private static DebugConsole _instance;
        private static readonly object _lock = new object();
        private bool _initialized = false;

        // Enable/disable logging globally
        private const bool ENABLE_DEBUG_LOGGING = false;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        private DebugConsole() { }

        public static DebugConsole Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new DebugConsole();
                    }
                }
                return _instance;
            }
        }

        public void Init()
        {
            if (!_initialized && ENABLE_DEBUG_LOGGING)
            {
                AllocConsole();
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                _initialized = true;
                Console.WriteLine("[DebugConsole] Initialized.");
            }
        }

        public void WriteLine(string message)
        {
            if (!ENABLE_DEBUG_LOGGING) return;

            if (!_initialized) Init();

            Console.WriteLine(message);
        }

        public void Close()
        {
            if (_initialized)
            {
                FreeConsole();
                _initialized = false;
            }
        }
    }
}











//using System;
//using System.Diagnostics;
//using System.IO;
//using System.Runtime.InteropServices;
//using System.Text;
//using Microsoft.Win32.SafeHandles;

//namespace SuperUtils
//{
//    public sealed class DebugConsole
//    {
//        private static DebugConsole _instance;
//        private static readonly object _lock = new object();

//        private bool _initialized = false;

//        private const bool ENABLE_DEBUG_LOGGING = true;

//        private DebugConsole() { }

//        public static DebugConsole Instance
//        {
//            get
//            {
//                if (_instance == null)
//                {
//                    lock (_lock)
//                    {
//                        _instance ??= new DebugConsole();
//                    }
//                }
//                return _instance;
//            }
//        }

//        public void Init()
//        {
//            WriteLine("[DebugConsole] Initialized.");
//        }

//        public void WriteLine(string message)
//        {
//            if (!ENABLE_DEBUG_LOGGING) return;

//            Debug.WriteLine(message);
//        }

//        public void Close()
//        {
//            if (_initialized)
//            {
//                Console.WriteLine("[DebugConsole] Closing...");
//                //_writer?.Flush();
//                //_writer?.Dispose();

//                //FreeConsole();
//                _initialized = false;
//            }
//        }
//    }
//}








//using System;
//using System.IO;
//using System.Runtime.InteropServices;
//using System.Text;
//using Microsoft.Win32.SafeHandles;

//namespace SuperUtils
//{
//    public sealed class DebugConsole
//    {
//        private static DebugConsole _instance;
//        private static readonly object _lock = new object();

//        private bool _initialized = false;
//        private StreamWriter _writer;

//        private const bool ENABLE_DEBUG_LOGGING = true;

//        [DllImport("kernel32.dll", SetLastError = true)]
//        private static extern bool AllocConsole();

//        [DllImport("kernel32.dll", SetLastError = true)]
//        private static extern bool FreeConsole();

//        [DllImport("kernel32.dll")]
//        private static extern IntPtr GetStdHandle(int nStdHandle);

//        private const int STD_OUTPUT_HANDLE = -11;

//        private DebugConsole() { }

//        public static DebugConsole Instance
//        {
//            get
//            {
//                if (_instance == null)
//                {
//                    lock (_lock)
//                    {
//                        _instance ??= new DebugConsole();
//                    }
//                }
//                return _instance;
//            }
//        }

//        public void Init()
//        {
//            if (_initialized || !ENABLE_DEBUG_LOGGING)
//                return;

//            AllocConsole();

//            var stdOutHandle = GetStdHandle(STD_OUTPUT_HANDLE);
//            var safeHandle = new SafeFileHandle(stdOutHandle, ownsHandle: false);
//            var fileStream = new FileStream(safeHandle, FileAccess.Write);
//            _writer = new StreamWriter(fileStream, new UTF8Encoding(false)) { AutoFlush = true };

//            // Set as Console.Out only once
//            Console.SetOut(_writer);

//            _initialized = true;
//            _writer.WriteLine("[DebugConsole] Initialized.");
//        }

//        public void WriteLine(string message)
//        {
//            if (!ENABLE_DEBUG_LOGGING) return;

//            if (!_initialized) Init();

//            _writer?.WriteLine(message);
//        }

//        public void Close()
//        {
//            if (_initialized)
//            {
//                _writer?.WriteLine("[DebugConsole] Closing...");
//                _writer?.Flush();
//                _writer?.Dispose();

//                FreeConsole();
//                _initialized = false;
//            }
//        }
//    }
//}
