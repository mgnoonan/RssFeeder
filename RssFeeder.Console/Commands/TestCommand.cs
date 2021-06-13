using Autofac;
using Oakton;
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
        }

        public override bool Execute(TestInput input)
        {
            var webUtils = _container.Resolve<IWebUtils>();
            var url = "https://i1.wp.com/gutsmack.com/wp-content/uploads/2021/06/Screen-Shot-2021-06-13-at-2.51.40-PM.png";

            webUtils.SaveUrlToDisk(
                url,
                "",
                "test.png");

            var contentType = webUtils.GetContentType(url);
            Log.Information("Test content type: {contentType}", contentType);

            Log.CloseAndFlush();

            // Just telling the OS that the command
            // finished up okay
            return true;
        }
    }
}
