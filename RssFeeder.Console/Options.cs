using CommandLine;

namespace RssFeeder.Console
{
    public class Options
    {
        [Option('t', "title")]
        public string Title { get; set; }

        [Option('u', "url")]
        public string Url { get; set; }

        [Option('d', "description")]
        public string Description { get; set; }

        [Option('f', "filename")]
        public string Filename { get; set; }

        [Option('l', "language")]
        public string Language { get; set; }

        [Option('o', "offline")]
        public bool IsOffline { get; set; }

        [Option('p', "customparser")]
        public string CustomParser { get; set; }

        [Option('h', "filters", Default = new string[] { })]
        public string[] Filters { get; set; }

        [Option('g', "config", Default = null, HelpText = "The name of the config.json file that contains all the options for the current run")]
        public string Config { get; set; }
    }
}
