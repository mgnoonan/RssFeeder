using System.IO;
using Autofac;
using HtmlAgilityPack;
using Oakton;
using RssFeeder.Console.FeedBuilders;
using RssFeeder.Console.Models;
using RssFeeder.Console.Utility;
using Serilog;

namespace RssFeeder.Console.Commands
{
    [Description("Test building the named feed")]
    public class TestCommand : OaktonCommand<TestInput>
    {
        private readonly IContainer _container;

        public TestCommand(IContainer container)
        {
            _container = container;

            // The usage pattern definition here is completely
            // optional
            Usage("Test build using feed URL and feed builder class").Arguments(x => x.FeedUrl, x => x.FeedBuilder);
        }

        public override bool Execute(TestInput input)
        {
            var webUtils = _container.Resolve<IWebUtils>();
            var builder = _container.ResolveNamed<IRssFeedBuilder>(input.FeedBuilder);

            string html = webUtils.DownloadStringWithCompression(input.FeedUrl);

            var doc = new HtmlDocument();
            doc.Load(new StringReader(html));

            builder.ParseRssFeedItems("test-command", input.FeedUrl, null, html);
            Log.CloseAndFlush();

            // Just telling the OS that the command
            // finished up okay
            return true;
        }
    }
}
