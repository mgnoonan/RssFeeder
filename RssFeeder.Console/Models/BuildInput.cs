using Oakton;

namespace RssFeeder.Console.Models
{
    public class BuildInput
    {
        [Description("The configuration file to build the RSS feeds")]
        public string ConfigFile { get; set; }
    }
}
