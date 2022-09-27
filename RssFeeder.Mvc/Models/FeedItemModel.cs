namespace RssFeeder.Mvc.Models;

public class FeedItemModel
{
    public string id { get; set; }
    public string FeedId { get; set; }
    public string Url { get; set; }
    public string UrlHash { get; set; }
    public string ImageUrl { get; set; }
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public string ArticleText { get; set; }
    public string Description { get; set; }
    public string MetaDescription { get; set; }
    public DateTime DateAdded { get; set; }
    public string FileName { get; set; }
    public string SiteName { get; set; }

}
