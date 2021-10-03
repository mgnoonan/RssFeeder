using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace RssFeeder.Console.Utility
{
    public interface IUtils
    {
        void SaveTextToDisk(string text, string filepath, bool deleteIfExists);
        void PurgeStaleFiles(string folderPath, short maximumAgeInDays);
        string CreateMD5Hash(string url);
        string GetAssemblyDirectory();
    }
}