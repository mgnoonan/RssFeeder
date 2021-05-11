using Oakton;

namespace RssFeeder.Console.Models
{
    public class TestInput
    {
        [Description("The feed URL to build")]
        public string FeedUrl { get; set; }
        [Description("The feed builder to use")]
        public string FeedBuilder { get; set; }
    }
}
