using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SuperUtils
{
    internal class StorageManager
    {
        public static StorageManager Instance => _instance;
        private static readonly StorageManager _instance = new();

        public List<string> SaveIncomingSuperParcel(SuperParcel parcel)
        {
            DebugConsole.Instance.WriteLine($"[SaveIncomingSuperParcel] Start saving parcel");

            return parcel.GetAllItems()
                .Select(item =>
                {
                    DebugConsole.Instance.WriteLine($"[SaveIncomingSuperParcel] Start saving file with MIME type: {item.MimeType}");
                    return SaveBytesToFile(item.GetBytes(), item.MimeType);
                })
                .ToList();
        }

        public string SaveBytesToFile(byte[] bytes, string mimeType)
        {
            DebugConsole.Instance.WriteLine($"[SaveBytesToFile] Start saving file with MIME type: {mimeType}, bytes length: {bytes.Length}");

            string ext = MimeHelper.GetExtension(mimeType);
            DebugConsole.Instance.WriteLine($"[SaveBytesToFile] Resolved extension: {ext}");

            string folder = GetWorkingFolder(mimeType);
            DebugConsole.Instance.WriteLine($"[SaveBytesToFile] Resolved working folder: {folder}");

            string fileName = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ext;
            string path = Path.Combine(folder, fileName);
            DebugConsole.Instance.WriteLine($"[SaveBytesToFile] Final path: {path}");

            try
            {
                Directory.CreateDirectory(folder);
                DebugConsole.Instance.WriteLine($"[SaveBytesToFile] Ensured directory exists: {folder}");

                File.WriteAllBytes(path, bytes);
                DebugConsole.Instance.WriteLine($"[SaveBytesToFile] Successfully saved file at: {path}");

                return path;
            }
            catch (Exception ex)
            {
                DebugConsole.Instance.WriteLine($"[SaveBytesToFile] Failed to save file: {ex.Message}");
                return "";
            }
        }


        public string GetWorkingFolder(string mimeType)
        {
            string typePrefix = mimeType.Split('/').FirstOrDefault() ?? "";
            return typePrefix switch
            {
                "text" => "text",
                "image" => "images",
                "video" => "videos",
                _ => "others"
            };
        }
    }
}
