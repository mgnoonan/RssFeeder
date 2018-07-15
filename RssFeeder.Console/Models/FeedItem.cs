using System;
using System.Web;

namespace RssFeeder.Console.Models
{
    public class FeedItem
    {
        public string Id { get; set; }
        public int FeedId { get; set; }
        public string Url { get; set; }
        public string UrlHash { get; set; }
        public string ImageUrl { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DateAdded { get; set; }
        public string FileName { get; set; }
        public string SiteName { get; set; }

        public string EncodedDescription
        {
            get
            {
                return HttpUtility.HtmlEncode(Description);
            }
        }

        public string FormattedDateAdded
        {
            get
            {
                return DateAdded.ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'");
            }
        }
    }
}
