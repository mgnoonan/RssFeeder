namespace RssFeeder.Console.Models;

public record ParseInput
{
    [Description("The URL to parse")]
    public string Url { get; init; }

    [Description("The article parser to use")]
    public string Parser { get; init; } = "adaptive-parser";

    [Description("The selector for the body of the article")]
    public string BodySelector { get; init; } = "article";

    [Description("The selector for the article text paragraphs contained within the body selector")]
    public string ParagraphSelector { get; init; } = "p";
}
