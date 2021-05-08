using System.IO;
using Autofac;
using HtmlAgilityPack;
using Oakton;
using RssFeeder.Console.Models;
using RssFeeder.Console.Parsers;
using RssFeeder.Console.Utility;
using Serilog;

namespace RssFeeder.Console.Commands
{
    [Description("Output meta data about the test URL")]
    public class TestCommand : OaktonCommand<TestInput>
    {
        private readonly IContainer _container;

        public TestCommand(IContainer container)
        {
            _container = container;

            // The usage pattern definition here is completely
            // optional
            Usage("Test URL using default parser").Arguments(x => x.Url);
            Usage("Test URL using specified parser").Arguments(x => x.Url, x => x.Parser);
            Usage("Test URL using specified parser and selectors").Arguments(x => x.Url, x => x.Parser, x => x.BodySelector, x => x.ParagraphSelector);
        }

        public override bool Execute(TestInput input)
        {
            var utils = _container.Resolve<IUtils>();
            var webUtils = _container.Resolve<IWebUtils>();
            var parser = _container.ResolveNamed<IArticleParser>(input.Parser);

            string urlHash = utils.CreateMD5Hash(input.Url);
            string html = webUtils.DownloadStringWithCompression(input.Url);

            var doc = new HtmlDocument();
            doc.Load(new StringReader(html));

            Log.Information("MD5 Hash = '{UrlHash}'", urlHash);
            Log.Information("og:site_name = '{SiteName}'", ParseMetaTagAttributes(doc, "og:site_name", "content").ToLower());
            Log.Information("og:title = '{Title}'", ParseMetaTagAttributes(doc, "og:title", "content"));
            Log.Information("og:description = '{Description}'", ParseMetaTagAttributes(doc, "og:description", "content"));
            Log.Information("og:image = '{Image}'", ParseMetaTagAttributes(doc, "og:image", "content"));

            string articleText = parser.GetArticleBySelector(doc.Text, input.BodySelector, input.ParagraphSelector);
            Log.Information("Article text = '{ArticleText}'", articleText);
            Log.CloseAndFlush();

            // Just telling the OS that the command
            // finished up okay
            return true;
        }

        private string ParseMetaTagAttributes(HtmlDocument doc, string property, string attribute)
        {
            // Retrieve the requested meta tag by property name
            var node = doc.DocumentNode.SelectSingleNode($"//meta[@property='{property}']");

            // Node can come back null if the meta tag is not present in the DOM
            // Attribute can come back null as well if not present on the meta tag
            string value = node?.Attributes[attribute]?.Value.Trim() ?? string.Empty;

            return value;
        }
    }
}
