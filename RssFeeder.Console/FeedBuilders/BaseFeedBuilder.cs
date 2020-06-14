using Serilog;

namespace RssFeeder.Console.FeedBuilders
{
    class BaseFeedBuilder
    {
        private readonly ILogger log;

        public BaseFeedBuilder(ILogger logger)
        {
            log = logger;
        }
    }
}
