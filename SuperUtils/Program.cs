
namespace SuperUtils
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            DebugConsole.Instance.Init();
            Task.Run(() => TcpConnectionService.Instance.Init()); 
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}