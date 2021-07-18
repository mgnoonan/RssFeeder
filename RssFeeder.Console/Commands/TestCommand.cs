using Autofac;
using HtmlAgilityPack;
using Oakton;
using RssFeeder.Console.Models;
using RssFeeder.Console.Parsers;
using RssFeeder.Console.Utility;
using Serilog;

namespace RssFeeder.Console.Commands
{
    [Description("Test building the named feed")]
    public class TestCommand : OaktonCommand<TestInput>
    {
        private readonly IContainer _container;

        public CrawlerConfig CrawlerConfig { get; set; }

        public TestCommand(IContainer container)
        {
            _container = container;
        }

        public override bool Execute(TestInput input)
        {
            var webUtils = _container.Resolve<IWebUtils>();
            var parser = _container.ResolveNamed<IArticleParser>("htmltag-parser");

            Log.Information("Crawler config: @CrawlerConfig", CrawlerConfig);
            Log.CloseAndFlush();

            // Just telling the OS that the command
            // finished up okay
            return true;
        }
    }
}
