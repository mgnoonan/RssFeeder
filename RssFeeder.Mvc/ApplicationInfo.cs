using System.IO;
using Microsoft.Extensions.Hosting;

namespace RssFeeder.Mvc;

public class AppVersionInfo
{
    private static readonly string _buildFileName = ".buildinfo.json";
    private readonly string _buildFilePath;
    private string _buildNumber;
    private string _gitHash;
    private string _gitShortHash;
    private string _workflow;

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

                    // First line is build number, second is workflow, third is commit hash
                    if (fileContents.Count > 0)
                    {
                        _buildNumber = fileContents[0];
                    }
                    if (fileContents.Count > 1)
                    {
                        _workflow = fileContents[1];
                    }
                    if (fileContents.Count > 2)
                    {
                        _gitHash = fileContents[2];
                    }
                }

                if (string.IsNullOrEmpty(_buildNumber))
                {
                    _buildNumber = "1";
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
                        _workflow = fileContents[1];
                    }
                    if (fileContents.Count > 2)
                    {
                        _gitHash = fileContents[2];
                    }
                }
                else
                {
                    _gitHash = "1bdc1de1a75b3a378e4cd1ee8a83da2256c52b69";
                }
            }

            return _gitHash;
        }
    }

    public string Workflow
    {
        get
        {
            if (string.IsNullOrEmpty(_workflow))
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
                        _workflow = fileContents[1];
                    }
                    if (fileContents.Count > 2)
                    {
                        _gitHash = fileContents[2];
                    }
                }
                else
                {
                    _workflow = "RssFeederMvc App Service CI/CD";
                }
            }

            return _workflow;
        }
    }

    public string ShortGitHash
    {
        get
        {
            if (string.IsNullOrEmpty(_gitShortHash))
            {
                _gitShortHash = GitHash[0..7];
            }

            return _gitShortHash;
        }
    }
}
