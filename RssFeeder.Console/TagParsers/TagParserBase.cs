namespace RssFeeder.Console.TagParsers;

public class TagParserBase
{
    protected string _sourceHtml;
    protected RssFeedItem _item;

    public void Initialize(string sourceHtml, RssFeedItem item)
    {
        _sourceHtml = sourceHtml;
        _item = item;
    }

    public virtual void PostParse()
    {
    }

    public virtual void PreParse()
    {
    }
}