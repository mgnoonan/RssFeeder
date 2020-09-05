using CommandLine;

namespace RssFeeder.Console
{
    public class Options
    {
        [Option('g', "config", Default = null, HelpText = "The name of the config.json file that contains all the options for the current run")]
        public string Config { get; set; }
    }
}
