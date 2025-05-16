using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SuperUtils
{
    public class SuperParcel
    {
        public class ParcelItem
        {
            public string MimeType { get; }
            public object RawData { get; }

            public ParcelItem(string mimeType, object rawData)
            {
                MimeType = mimeType;
                RawData = rawData;
            }

            public byte[] GetBytes()
            {
                return ConvertDataToBytes(RawData);
            }
        }

        private readonly List<ParcelItem> _items = new();

        public void Clear()
        {
            _items.Clear();
        }

        public void AddItem(ParcelItem parcelItem)
        {
            AddItem(parcelItem.MimeType, parcelItem.RawData);
        }

        public void AddItem(string mimeType, object rawData)
        {
            if (string.IsNullOrEmpty(mimeType) || rawData == null)
            {
                DebugConsole.Instance.WriteLine("addItem: Skipping because mimeType or data is null or empty");
                return;
            }

            DebugConsole.Instance.WriteLine($"addItem: mimeType = {mimeType}, data class = {rawData.GetType().Name}");
            _items.Add(new ParcelItem(mimeType, rawData));
            DebugConsole.Instance.WriteLine($"addItem: Item added. Total items now = {_items.Count}");
        }

        public List<ParcelItem> GetAllItems()
        {
            return _items.Select(it => new ParcelItem(it.MimeType, it.RawData)).ToList();
        }

        private byte[] GetBodyByteParcel()
        {
            using var bodyStream = new MemoryStream();

            foreach (var item in _items)
            {
                bodyStream.Write(Encoding.UTF8.GetBytes("--ITEM_START--"));
                bodyStream.Write(Encoding.UTF8.GetBytes($"MIMETYPE:{item.MimeType}"));
                bodyStream.Write(Encoding.UTF8.GetBytes("CONTENT:"));
                bodyStream.Write(item.GetBytes());
                bodyStream.Write(Encoding.UTF8.GetBytes("--ITEM_END--\n"));
            }
            bodyStream.Write(Encoding.UTF8.GetBytes("END_OF_TRANSMISSION\n"));

            return bodyStream.ToArray();
        }

        public byte[] GetFullByteParcel()
        {
            var bodyBytes = GetBodyByteParcel();
            var checksum = ComputeSHA256(bodyBytes);

            var headerBuilder = new StringBuilder();
            headerBuilder.Append("[SUPERUTILS_PARCEL]\n");
            headerBuilder.Append("VERSION:1\n");
            headerBuilder.Append($"TOTAL_SIZE:{bodyBytes.Length}\n");
            headerBuilder.Append($"NUM_ITEMS:{_items.Count}\n");
            headerBuilder.Append($"CHECKSUM:{checksum}\n");
            headerBuilder.Append("END_HEADER\n");

            var headerBytes = Encoding.UTF8.GetBytes(headerBuilder.ToString());

            using var finalStream = new MemoryStream();
            finalStream.Write(headerBytes);
            finalStream.Write(bodyBytes);

            return finalStream.ToArray();
        }

        public static Result<SuperParcel> FromParcelBytes(byte[] parcelBytes)
        {
            int CopyTillCharSequence(int srcStart, byte[] src, MemoryStream dest, string sequence)
            {
                var sequenceTracker = new CharSequenceTracker(Encoding.UTF8.GetBytes(sequence));

                int findIndex = srcStart;
                while (!sequenceTracker.Found())
                {
                    sequenceTracker.NextChar(src[findIndex], findIndex);
                    findIndex++;
                }

                for (int i = srcStart; i < sequenceTracker.RelativeStart; i++)
                {
                    dest.WriteByte(src[i]);
                }

                return sequenceTracker.RelativeStart - 1;
            }

            var parsed = new SuperParcel();
            var checksumFromParcel = new MemoryStream();
            var currentMimeType = new MemoryStream();
            var currentParcelItemContent = new MemoryStream();

            int i = 0;
            var checksumSequence = new CharSequenceTracker(Encoding.UTF8.GetBytes("CHECKSUM:"));
            var itemStartSequence = new CharSequenceTracker(Encoding.UTF8.GetBytes("--ITEM_START--"));
            var mimeTypeSequence = new CharSequenceTracker(Encoding.UTF8.GetBytes("MIMETYPE:"));
            var contentSequence = new CharSequenceTracker(Encoding.UTF8.GetBytes("CONTENT:"));
            var endTransmissionSequence = new CharSequenceTracker(Encoding.UTF8.GetBytes("END_OF_TRANSMISSION\n"));

            while (i < parcelBytes.Length)
            {
                var b = parcelBytes[i];

                if (checksumSequence.Found())
                {
                    i = CopyTillCharSequence(i, parcelBytes, checksumFromParcel, "\nEND_HEADER");
                    checksumSequence.MarkComplete();
                }
                else
                {
                    checksumSequence.NextChar(b, i);
                }

                if (itemStartSequence.Found())
                {
                    if (mimeTypeSequence.Found())
                    {
                        i = CopyTillCharSequence(i, parcelBytes, currentMimeType, "CONTENT:");
                        mimeTypeSequence.MarkComplete();
                    }
                    else
                    {
                        mimeTypeSequence.NextChar(b, i);
                    }

                    if (contentSequence.Found())
                    {
                        i = CopyTillCharSequence(i, parcelBytes, currentParcelItemContent, "--ITEM_END--");

                        var mimeType = Encoding.UTF8.GetString(currentMimeType.ToArray());
                        var content = currentParcelItemContent.ToArray();

                        if (mimeType == MimeHelper.GetMimeType(MimeHelper.TXT))
                        {
                            parsed.AddItem(mimeType, Encoding.UTF8.GetString(content));
                        }
                        else
                        {
                            parsed.AddItem(mimeType, content);
                        }

                        itemStartSequence.Reset();
                        contentSequence.Reset();
                        mimeTypeSequence.Reset();

                        currentMimeType = new MemoryStream();
                        currentParcelItemContent = new MemoryStream();
                    }
                    else
                    {
                        contentSequence.NextChar(b, i);
                    }
                }
                else
                {
                    itemStartSequence.NextChar(b, i);
                }

                if (endTransmissionSequence.Found())
                {
                    break;
                }
                else
                {
                    endTransmissionSequence.NextChar(b, i);
                }

                i++;
            }

            if (!endTransmissionSequence.Found())
                return Result<SuperParcel>.Failure("Missing end of transmission");

            var hashed = ComputeSHA256(parsed.GetBodyByteParcel());
            if (hashed != Encoding.UTF8.GetString(checksumFromParcel.ToArray()))
                return Result<SuperParcel>.Failure("Mismatched SHA256 checksum");

            return Result<SuperParcel>.Success(parsed);
        }

        private static string ComputeSHA256(byte[] input)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(input);
            return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
        }

        private static byte[] ConvertDataToBytes(object data)
        {
            switch (data)
            {
                case string strData:
                    if (File.Exists(strData))
                        return File.ReadAllBytes(strData);
                    else
                        return Encoding.UTF8.GetBytes(strData);

                case FileInfo file:
                    if (file.Exists)
                        return File.ReadAllBytes(file.FullName);
                    else
                        throw new Exception($"File does not exist: {file.FullName}");

                case Bitmap bitmap:
                    {
                        using MemoryStream stream = new MemoryStream();
                        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        return stream.ToArray();
                    }

                case byte[] bytes:
                    return bytes;

                default:
                    throw new Exception($"Unrecognized data type: {data} ({data.GetType().Name})");
            }
        }
    }
}











//using System;
//using System.Buffers.Text;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel;
//using System.Diagnostics.Metrics;
//using System.Drawing.Imaging;
//using System.IO;
//using System.Reflection.Metadata;
//using System.Runtime.Intrinsics.Arm;
//using System.Security.Cryptography;
//using System.Security.Policy;
//using System.Security.Principal;
//using System.Text;
//using Microsoft.VisualBasic.ApplicationServices;
//using Microsoft.VisualBasic.Devices;
//using static System.Net.WebRequestMethods;
//using static System.Resources.ResXFileRef;
//using static System.Runtime.InteropServices.JavaScript.JSType;
//using static System.Windows.Forms.DataFormats;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement;
//using System.Windows.Forms;


//namespace SuperUtils
//{
//        /// <summary>
//        /// The `SuperParcel` class is designed to represent a parcel of data that can contain multiple items. Each item in the parcel has a MIME type and is encoded using base64 encoding to ensure that binary data (like files or images) can be transmitted as text. The process of creating a parcel, encoding its contents, and verifying its integrity using checksums is as follows:
//        ///- ** Parcel Structure**:
//        ///  The parcel consists of a header and a body:
//        ///    - ** Header**: Contains metadata such as the parcel version, total size of the body(in bytes), number of items, and a checksum for the body.The header ends with "END_HEADER".
//        ///    - **Body**: Contains the actual items, where each item is encapsulated by `--ITEM_START--` and `--ITEM_END--`. Each item has the following format:
//        ///      - `MIMETYPE: <mime-type>`: The MIME type of the data (e.g., "image/png", "application/pdf").
//        ///      - `ENCODING: base64`: Indicates that the data is base64-encoded.
//        ///      - `CONTENT:`: The base64-encoded data of the item.
//        ///    - After all items, the body ends with "END_OF_TRANSMISSION".

//        ///- ** Adding Items**:
//        ///  The `AddItem` method allows you to add items to the parcel.Each item is represented by a MIME type(string) and the actual data(of various types such as string, `System.Drawing.Image`, `byte[]`, or file path). The `ConvertDataToString` method is responsible for encoding the data based on its type:
//        ///    - If the data is a file path(and the file exists), it is read and base64-encoded.
//        ///    - If the data is an image(`System.Drawing.Image`), it is saved as a PNG and base64-encoded.
//        ///    - If the data is already a `byte[]`, it is directly base64-encoded.
//        ///    - If the data is a string, it is used directly(assuming it's plain text).

//        ///- **Base64 Encoding**:
//        ///  The base64 encoding is used to ensure that binary data (images, files, etc.) can be safely transmitted as ASCII text. Base64 encoding converts the raw binary data into a text string, which is compatible with textual protocols (like JSON, XML, HTTP).

//        ///- **Checksum (SHA-256)**:
//        ///  After all items are added to the parcel, the body(which contains the encoded data of all items) is hashed using the SHA-256 algorithm.The `ComputeSHA256` method takes the entire body string and computes its hash, which is then stored in the header under the `CHECKSUM` field.The checksum is used to verify the integrity of the parcel when it is received or parsed.If the parcel is altered during transmission(e.g., data corruption or tampering), the checksum won't match, indicating the parcel is invalid.

//        ///- ** Constructing the Full Parcel**:
//        ///  The `GetFullParcel` method constructs the final parcel by combining the header and body.The header contains metadata and the checksum, while the body contains the encoded data of each item.The final string represents the entire parcel, which can then be transmitted, stored, or parsed.

//        ///- **Parsing the Parcel**:
//        ///  The static `FromParcelString` method is responsible for parsing the parcel from its string representation.It validates the checksum to ensure the integrity of the parcel, and then it extracts each item, decoding the base64 data back into its original form.The MIME type of each item is also parsed and stored for later use.

//        ///- **Key Concepts**:
//        ///    1. **Base64 Encoding**: This ensures binary data can be safely represented as a text string, making it suitable for transmission over text-based protocols.
//        ///    2. **SHA-256 Hashing**: This is used to verify the integrity of the parcel. The hash is computed on the body (the actual data content) of the parcel, and if any changes occur in the body, the checksum will not match, indicating the parcel has been corrupted or tampered with.
//        ///    3. **Parcel Item Format**: Each item is separated by `--ITEM_START--` and `--ITEM_END--`, making it easy to separate and identify individual items when parsing the parcel.

//        ///- **Use Cases**:
//          ///This class can be useful for transmitting collections of data(such as images, files, or other media) as a single parcel.The base64 encoding ensures that binary data is compatible with text-based transmission systems, while the checksum guarantees that the data remains intact during transmission.
//        /// </summary>
//        internal class SuperParcel
//    {
//            private class ParcelItem
//            {
//                public string MimeType { get; }
//                public string EncodedData { get; }

//                public ParcelItem(string mimeType, string encodedData)
//                {
//                    MimeType = mimeType;
//                    EncodedData = encodedData;
//                }
//            }

//            private readonly List<ParcelItem> items = [];

//            public void AddItem(string mimeType, object data)
//            {
//                if (string.IsNullOrEmpty(mimeType) || data == null)
//                    return;

//                string encodedData = ConvertDataToString(data);
//                if (encodedData != null)
//                    items.Add(new ParcelItem(mimeType, encodedData));
//            }

//            public string GetFullParcel()
//            {
//                StringBuilder bodyBuilder = new();
//                foreach (var item in items)
//                {
//                    bodyBuilder.AppendLine("--ITEM_START--");
//                    bodyBuilder.AppendLine($"MIMETYPE:{item.MimeType}");
//                    bodyBuilder.AppendLine("ENCODING:base64");
//                    bodyBuilder.AppendLine("CONTENT:");
//                    bodyBuilder.AppendLine(item.EncodedData);
//                    bodyBuilder.AppendLine("--ITEM_END--");
//                }
//                bodyBuilder.AppendLine("END_OF_TRANSMISSION");

//                string body = bodyBuilder.ToString();
//                string checksum = ComputeSHA256(body);

//                StringBuilder headerBuilder = new();
//                headerBuilder.AppendLine("[SUPERUTILS_PARCEL]");
//                headerBuilder.AppendLine("VERSION:1");
//                headerBuilder.AppendLine($"TOTAL_SIZE:{Encoding.UTF8.GetByteCount(body)}");
//                headerBuilder.AppendLine($"NUM_ITEMS:{items.Count}");
//                headerBuilder.AppendLine($"CHECKSUM:{checksum}");
//                headerBuilder.AppendLine("END_HEADER");

//                return headerBuilder + body;
//            }

//            public void Clear()
//            {
//                items.Clear();
//            }

//            private string ConvertDataToString(object data)
//            {
//                switch (data)
//                {
//                    case string s when System.IO.File.Exists(s):
//                        return Convert.ToBase64String(System.IO.File.ReadAllBytes(s));
//                    case string s:
//                        return s;
//                    case System.Drawing.Image img:
//                        using (var ms = new MemoryStream())
//                        {
//                            img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
//                            return Convert.ToBase64String(ms.ToArray());
//                        }
//                    case byte[] byteArray:
//                        return Convert.ToBase64String(byteArray);
//                    default:
//                        return data.ToString();
//                }
//            }

//            private string ComputeSHA256(string input)
//            {
//                using var sha = SHA256.Create();
//                byte[] bytes = Encoding.UTF8.GetBytes(input);
//                byte[] hash = sha.ComputeHash(bytes);
//                return BitConverter.ToString(hash).Replace("-", "");
//            }

//            public static Result<SuperParcel> FromParcelString(string parcel)
//            {
//                try
//                {
//                    var lines = parcel.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
//                    var controller = new SuperParcel();
//                    bool readingHeader = true;
//                    string checksumFromHeader = null;
//                    var bodyBuilder = new StringBuilder();

//                    foreach (var line in lines)
//                    {
//                        if (readingHeader)
//                        {
//                            if (line.StartsWith("CHECKSUM:"))
//                                checksumFromHeader = line.Substring("CHECKSUM:".Length);
//                            if (line == "END_HEADER")
//                                readingHeader = false;
//                        }
//                        else
//                        {
//                            bodyBuilder.AppendLine(line);
//                        }
//                    }

//                    if (checksumFromHeader == null)
//                        return Result<SuperParcel>.Failure("Missing checksum in header.");

//                    string body = bodyBuilder.ToString();
//                    string computedChecksum = controller.ComputeSHA256(body);
//                    if (!string.Equals(computedChecksum, checksumFromHeader, StringComparison.OrdinalIgnoreCase))
//                        return Result<SuperParcel>.Failure("Checksum mismatch. Parcel may be corrupted.");

//                    var itemBlocks = body.Split(new[] { "--ITEM_START--" }, StringSplitOptions.RemoveEmptyEntries);
//                    foreach (var block in itemBlocks)
//                    {
//                        if (block.StartsWith("END_OF_TRANSMISSION"))
//                            continue;

//                        var blockLines = block.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
//                        string mimeType = null;
//                        var contentBuilder = new StringBuilder();
//                        bool readingContent = false;

//                        foreach (var bline in blockLines)
//                        {
//                            if (bline.StartsWith("MIMETYPE:"))
//                                mimeType = bline.Substring("MIMETYPE:".Length);
//                            else if (bline == "CONTENT:")
//                                readingContent = true;
//                            else if (bline == "--ITEM_END--")
//                                break;
//                            else if (readingContent)
//                                contentBuilder.AppendLine(bline);
//                        }

//                        if (mimeType == null)
//                            return Result<SuperParcel>.Failure("Malformed item block (missing MIMETYPE).");

//                        controller.items.Add(new ParcelItem(mimeType, contentBuilder.ToString().Trim()));
//                    }

//                    return Result<SuperParcel>.Success(controller);
//                }
//                catch (Exception ex)
//                {
//                    return Result<SuperParcel>.Failure($"Exception during parsing: {ex.Message}");
//                }
//            }
//        }
//}
