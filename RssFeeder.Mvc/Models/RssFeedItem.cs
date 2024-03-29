﻿namespace RssFeeder.Mvc.Models;

public class ExportFeedItems : RssFeedItem
{
    public ExportFeedItems() : base()
    { }
}

public class RssFeedItem
{
    public RssFeedItem()
    {
        FeedAttributes = new FeedAttributes();
    }

    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }
    public string FeedId { get; set; }
    public string Url { get; set; }
    public string UrlHash { get; set; }
    public string ImageUrl { get; set; }
    public string VideoUrl { get; set; }
    public int VideoHeight { get; set; }
    public int VideoWidth { get; set; }
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public string ArticleText { get; set; }
    public string Description { get; set; }
    public string MetaDescription { get; set; }
    public DateTime DateAdded { get; set; }
    public string FileName { get; set; }
    public string SiteName { get; set; }
    public string HostName { get; set; }
    public string LinkLocation { get; set; }
    public Dictionary<string, string> OpenGraphAttributes { get; set; }
    public Dictionary<string, string> HtmlAttributes { get; set; }
    public FeedAttributes FeedAttributes { get; set; }

    [JsonIgnore]
    public string EncodedDescription
    {
        get
        {
            return System.Web.HttpUtility.HtmlEncode(Description);
        }
    }

    [JsonIgnore]
    public string FormattedDateAdded
    {
        get
        {
            return DateAdded.ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'");
        }
    }
}

public class FeedAttributes
{
    public string FeedId { get; set; }
    public string Url { get; set; }
    public string UrlHash { get; set; }
    public string Title { get; set; }
    public DateTime DateAdded { get; set; }
    public string FileName { get; set; }
    public string LinkLocation { get; set; }
}
