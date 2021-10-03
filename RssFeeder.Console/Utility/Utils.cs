using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Serilog;

namespace RssFeeder.Console.Utility
{
    /// <summary>
    /// Summary description for Utility.
    /// </summary>
    public class Utils : IUtils
    {
        public void SaveTextToDisk(string text, string filepath, bool deleteIfExists)
        {
            try
            {
                if (deleteIfExists && File.Exists(filepath))
                {
                    File.Delete(filepath);
                }

                Log.Logger.Information("Saving text file '{filepath}'", filepath);

                // WriteAllText creates a file, writes the specified string to the file,
                // and then closes the file.    You do NOT need to call Flush() or Close().
                File.WriteAllText(filepath, text);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "SaveTextToDisk: Unexpected error '{message}'", ex.Message);
            }
        }

        public void PurgeStaleFiles(string folderPath, short maximumAgeInDays)
        {
            DateTime minimumDate = DateTime.Now.AddDays(-maximumAgeInDays);

            var files = Directory.EnumerateFiles(folderPath);
            int count = 0;

            foreach (var file in files)
            {
                if (DeleteFileIfOlderThan(file, minimumDate))
                {
                    count++;
                }
            }

            Log.Logger.Information("Removed {count} files older than {maximumAgeInDays} days from {folderPath}", count, maximumAgeInDays, folderPath);
        }

        private bool DeleteFileIfOlderThan(string path, DateTime date)
        {
            var file = new FileInfo(path);
            if (file.CreationTime < date)
            {
                Log.Logger.Information("Removing {fileName}", file.FullName);
                file.Delete();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Transform the incoming url to an MD5 hash code
        /// </summary>
        /// <param name="url">The url to transform</param>
        /// <returns>A 32 character hash code</returns>
        public string CreateMD5Hash(string url)
        {
            char[] cs = url.ToLowerInvariant().ToCharArray();
            byte[] buffer = new byte[cs.Length];
            for (int i = 0; i < cs.Length; i++)
                buffer[i] = (byte)cs[i];

            MD5 md5 = MD5.Create();
            byte[] output = md5.ComputeHash(buffer);

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < output.Length; i++)
                builder.AppendFormat("{0:x2}", output[i]);

            return builder.ToString();
        }

        public string GetAssemblyDirectory()
        {
            var type = new Utils();
            var assembly = Assembly.GetAssembly(type.GetType());
            string location = assembly.Location;
            var uri = new UriBuilder(location);
            string path = Uri.UnescapeDataString(uri.Path);

            // Add the trailing backslash if not present
            string name = Path.GetDirectoryName(path);
            if (!name.EndsWith("\\"))
                name += "\\";

            return name;
        }
    }
}
