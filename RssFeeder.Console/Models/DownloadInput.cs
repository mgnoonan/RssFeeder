using Oakton;

namespace RssFeeder.Console.Models
{
    [Description("Use the Web Driver to test download from a URL")]
    public class DownloadInput
    {
        [Description("The URL to download")]
        public string Url { get; set; }

        [Description("Flag to take a screen capture")]
        [FlagAlias("capture", 'c')]
        public bool CaptureFlag { get; set; }
    }
}
