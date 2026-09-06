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
        JsonLdObjects = new List<Newtonsoft.Json.Linq.JObject>();
    }

    public string Id { get; set; }
    public string SiteName { get; set; }
    public string HostName { get; set; }
    public Guid RunId { get; set; }
    public Dictionary<string, string> OpenGraphAttributes { get; set; }
    public Dictionary<string, string> HtmlAttributes { get; set; }
    /// <summary>
    /// Parsed JSON-LD objects extracted from the HTML (each entry is a JObject parsed from a
    /// &lt;script type="application/ld+json"&gt; block). Stored so the structured JSON-LD is
    /// persisted with the item in the database.
    /// </summary>
    public List<Newtonsoft.Json.Linq.JObject> JsonLdObjects { get; set; }
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
    public bool IsHeadline { get; set; }
}
