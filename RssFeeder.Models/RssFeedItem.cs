using System;
using System.Collections.Generic;

namespace RssFeeder.Models;

public record RssFeedItem
{
    public RssFeedItem()
    {
        FeedAttributes = new FeedAttributes();
        OpenGraphAttributes = new Dictionary<string, string>();
        HtmlAttributes = new Dictionary<string, string>();
    }

    public string Id { get; set; }
    public string SiteName { get; set; }
    public string HostName { get; set; }
    public Dictionary<string, string> OpenGraphAttributes { get; set; }
    public Dictionary<string, string> HtmlAttributes { get; set; }
    public FeedAttributes FeedAttributes { get; set; }
}

public record FeedAttributes
{
    public string FeedId { get; set; }
    public string Url { get; set; }
    public string UrlHash { get; set; }
    public string Title { get; set; }
    public DateTime DateAdded { get; set; }
    public string FileName { get; set; }
    public string LinkLocation { get; set; }
    public bool IsUrlShortened { get; set; }
}
