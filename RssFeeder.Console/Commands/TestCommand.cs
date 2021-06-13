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

        public TestCommand(IContainer container)
        {
            _container = container;
        }

        public override bool Execute(TestInput input)
        {
            var webUtils = _container.Resolve<IWebUtils>();
            var parser = _container.ResolveNamed<IArticleParser>("htmltag-parser");
            var url = "https://rumble.com/vifqub-state-trooper-flips-pregnant-womans-car-over-traffic-stop-this-is-where-you.html";
            var fileName = "test.html";

            webUtils.WebDriverUrlToDisk(
                url,
                "",
                fileName);

            var doc = new HtmlDocument();
            doc.Load(fileName);
            var result = parser.GetArticleBySelector(doc.Text, "#videoPlayer", "video");
            Log.Information("Article parsed: '{result}'", result);

            Log.CloseAndFlush();

            // Just telling the OS that the command
            // finished up okay
            return true;
        }
    }
}
