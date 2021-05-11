using Oakton;

namespace RssFeeder.Console.Models
{
    public class ParseInput
    {
        [Description("The URL to parse")]
        public string Url { get; set; }

        [Description("The article parser to use")]
        public string Parser { get; set; } = "adaptive-parser";

        [Description("The selector for the body of the article")]
        public string BodySelector { get; set; } = "article";

        [Description("The selector for the article text paragraphs contained within the body selector")]
        public string ParagraphSelector { get; set; } = "p";
    }
}
