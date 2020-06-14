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

        [Option('l', "language")]
        public string Language { get; set; }

        [Option('p', "customparser")]
        public string CustomParser { get; set; }

        [Option('h', "filters", Default = new string[] { })]
        public string[] Filters { get; set; }

        [Option('g', "config", Default = null, HelpText = "The name of the config.json file that contains all the options for the current run")]
        public string Config { get; set; }

        [Option("output-file", Default = null)]
        public string OutputFile { get; set; }

        [Option("test-definition", Default = null)]
        public string TestDefinition { get; set; }

        [Option("collection-name", Default = null)]
        public string CollectionName { get; set; }
    }
}
