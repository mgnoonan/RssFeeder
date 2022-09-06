using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Hosting;

public class AppVersionInfo
{
    private static readonly string _buildFileName = ".buildinfo.json";
    private string _buildFilePath;
    private string _buildNumber;
    private string _gitHash;
    private string _gitShortHash;

    public AppVersionInfo(IHostEnvironment hostEnvironment)
    {
        _buildFilePath = Path.Combine(hostEnvironment.ContentRootPath, _buildFileName);
    }

    public string BuildNumber
    {
        get
        {
            // Build number format should be yyyyMMdd.# (e.g. 20200308.1)
            if (string.IsNullOrEmpty(_buildNumber))
            {
                if (File.Exists(_buildFilePath))
                {
                    var fileContents = File.ReadLines(_buildFilePath).ToList();

                    // First line is build number, second is commit hashs
                    if (fileContents.Count > 0)
                    {
                        _buildNumber = fileContents[0];
                    }
                    if (fileContents.Count > 1)
                    {
                        _gitHash = fileContents[1];
                    }
                }

                if (string.IsNullOrEmpty(_buildNumber))
                {
                    _buildNumber = DateTime.UtcNow.ToString("yyyyMMdd") + ".0";
                }
            }

            return _buildNumber;
        }
    }

    public string GitHash
    {
        get
        {
            if (string.IsNullOrEmpty(_gitHash))
            {
                if (File.Exists(_buildFilePath))
                {
                    var fileContents = File.ReadLines(_buildFilePath).ToList();

                    // First line is build number, second is commit hashs
                    if (fileContents.Count > 0)
                    {
                        _buildNumber = fileContents[0];
                    }
                    if (fileContents.Count > 1)
                    {
                        _gitHash = fileContents[1];
                    }
                }
            }

            return _gitHash;
        }
    }

    public string ShortGitHash
    {
        get
        {
            if (!string.IsNullOrEmpty(_gitShortHash))
            {
                _gitShortHash = GitHash.Substring(GitHash.Length - 6, 6);
            }
            
            return _gitShortHash;
        }
    }
}