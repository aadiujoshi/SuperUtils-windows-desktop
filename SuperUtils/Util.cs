using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SuperUtils
{
    internal class Util
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr ShellExecute(
            IntPtr hwnd,
            string lpOperation,
            string lpFile,
            string lpParameters,
            string lpDirectory,
            int nShowCmd);

        public static void RunScript(string operation, string script)
        {
            ShellExecute(IntPtr.Zero, operation, script, null, null, 1);
        }

        public static void OpenFolderInExplorer(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                throw new ArgumentException("Folder path is null or empty.", nameof(folderPath));

            string fullPath = Path.GetFullPath(folderPath);

            ShellExecute(IntPtr.Zero, "open", fullPath, null, null, 1);
        }

        public static void OpenAppFolderInExplorer()
        {
            string folderPath = Application.StartupPath; // Gets the application's folder
            Process.Start("explorer.exe", folderPath);
        }

        public static List<FileInfo> SelectFilesWithDialog()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select files";
                ofd.Filter = "All Supported Files|*.txt;*.html;*.htm;*.jpg;*.jpeg;*.png;*.bmp;*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.zip;*.rar;*.mp3;*.mp4|" +
                             "Text Files|*.txt;*.html;*.htm|" +
                             "Image Files|*.jpg;*.jpeg;*.png;*.bmp|" +
                             "PDF Files|*.pdf|" +
                             "Word Documents|*.doc;*.docx|" +
                             "Excel Spreadsheets|*.xls;*.xlsx|" +
                             "Compressed Files|*.zip;*.rar|" +
                             "Audio Files|*.mp3|" +
                             "Video Files|*.mp4";
                ofd.Multiselect = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var files = new List<FileInfo>();

                    foreach (string filePath in ofd.FileNames)
                    {
                        var fileInfo = new FileInfo(filePath);
                        files.Add(fileInfo);

                        DebugConsole.Instance.WriteLine($"Selected file: {fileInfo.FullName}");
                    }

                    DebugConsole.Instance.WriteLine($"Total files selected: {files.Count}");
                    return files;
                }
            }

            return new List<FileInfo>(); 
        }
    }

    internal static class WinformsUtil
    {
        public static void SafeInvoke(this Control control, Action action)
        {
            if (control.InvokeRequired)
                control.BeginInvoke(action);
            else
                action();
        }
    }

    internal class ClipboardUtils
    {
        public static void CopyFilesToClipboard(List<string> filePaths)
        {
            if (filePaths == null || filePaths.Count == 0)
                return;

            var thread = new Thread(() =>
            {
                foreach (var filePath in filePaths)
                {
                    if (!File.Exists(filePath))
                        continue;

                    var mimeType = MimeHelper.GetMimeType(filePath);

                    if (mimeType.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
                    {
                        var text = File.ReadAllText(filePath);
                        Clipboard.SetText(text);
                    }
                    else if (mimeType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var image = Image.FromFile(filePath))
                        {
                            Clipboard.SetImage(image);
                        }
                    }
                    else
                    {
                        // Unsupported file type - do nothing
                    }
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}
