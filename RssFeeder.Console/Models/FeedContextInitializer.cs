//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Data.Entity;
//using RSSFeed.Models;

//namespace RSSFeed.Models
//{
//    class FeedContextInitializer : DropCreateDatabaseIfModelChanges<FeedContext>
//    {
//        protected override void Seed(FeedContext context)
//        {
//            context.RssFeeds.Add(new Feed
//            {
//                Title = "The Drudge Report",
//                Url = "http://www.drudgereport.com",
//                Description = "The Drudge Report",
//#if DEBUG
//                Filename = @"drudge.xml",
//#else
//                Filename = @"C:\Webs\old\content\Rss\drudge.xml",
//#endif
//                Language = "en-US",
//                CustomParser = "RSSFeed.CustomBuilders.DrudgeReportFeedBuilder"
//            });
//            //            context.RssFeeds.Add(new RssFeed
//            //            {
//            //                Title = "MSDN",
//            //                Url = "http://msdn.microsoft.com/rss.xml",
//            //                Description = "MSDN",
//            //#if DEBUG
//            //                Filename = @"msdn.xml",
//            //#else
//            //                Filename = @"C:\Webs\old\content\Rss\msdn.xml",
//            //#endif
//            //                Language = "en-US",
//            //                CustomParser = null
//            //            });
//            context.RssFeeds.Add(new Feed
//            {
//                Title = "Netflix",
//                Url = "http://rss.netflix.com/QueueRSS?id=P1717123491843882065222688160957068",
//                Description = "Netflix",
//#if DEBUG
//                Filename = @"netflix.xml",
//#else
//                Filename = @"C:\Webs\old\content\Rss\netflix.xml",
//#endif
//                Language = "en-US",
//                CustomParser = null
//            });
//            //            context.RssFeeds.Add(new RssFeed
//            //            {
//            //                Title = "Boortz",
//            //                Url = "http://boortz.com/mp3/archive/feed.rss",
//            //                Description = "Boortz",
//            //#if DEBUG
//            //                Filename = @"boortz.xml",
//            //#else
//            //                Filename = @"C:\Webs\old\content\Rss\boortz.xml",
//            //#endif
//            //                Language = "en-US",
//            //                CustomParser = null
//            //            });
//            context.RssFeeds.Add(new Feed
//            {
//                Title = "Dolphins",
//                Url = "http://www.miamiherald.com/sports/football/index.xml",
//                Description = "Dolphins",
//#if DEBUG
//                Filename = @"dolphins.xml",
//#else
//                Filename = @"C:\Webs\old\content\Rss\dolphins.xml",
//#endif
//                Language = "en-US",
//                CustomParser = null
//            });
//            context.RssFeeds.Add(new Feed
//            {
//                Title = "Yankees",
//                Url = "http://partner.mlb.com/partnerxml/gen/news/rss/nyy.xml",
//                Description = "Yankees",
//#if DEBUG
//                Filename = @"yankees.xml",
//#else
//                Filename = @"C:\Webs\old\content\Rss\yankees.xml",
//#endif
//                Language = "en-US",
//                CustomParser = null
//            });
//            context.RssFeeds.Add(new Feed
//            {
//                Title = "ESPN",
//                Url = "http://sports.espn.go.com/espn/rss/news",
//                Description = "ESPN",
//#if DEBUG
//                Filename = @"espn.xml",
//#else
//                Filename = @"C:\Webs\old\content\Rss\espn.xml",
//#endif
//                Language = "en-US",
//                CustomParser = null
//            });
//            context.RssFeeds.Add(new Feed
//            {
//                Title = "Fox News",
//                Url = "http://feeds.foxnews.com/foxnews/latest",
//                Description = "Fox News",
//#if DEBUG
//                Filename = @"fox.xml",
//#else
//                Filename = @"C:\Webs\old\content\Rss\fox.xml",
//#endif
//                Language = "en-US",
//                CustomParser = null
//            });
//            context.RssFeeds.Add(new Feed
//            {
//                Title = "Dayton Daily News",
//                Url = "http://www.daytondailynews.com/n/content/oh/services/rss/ddn/localnews.xml",
//                Description = "Dayton Daily News",
//#if DEBUG
//                Filename = @"ddn.xml",
//#else
//                Filename = @"C:\Webs\old\content\Rss\ddn.xml",
//#endif
//                Language = "en-US",
//                CustomParser = null
//            });

//            context.FeedFilters.Add(new FeedFilter
//            {
//                FeedId = 1,
//                UrlHash = "fb27ce207f3ca32d97999d182ec93576"
//            });
//            context.FeedFilters.Add(new FeedFilter
//            {
//                FeedId = 1,
//                UrlHash = "0cc6fcfe73c643623766047524ab10e5"
//            });
//            context.FeedFilters.Add(new FeedFilter
//            {
//                FeedId = 1,
//                UrlHash = "7253ad8ec5f5bb097257386900898e15"
//            });
//            context.FeedFilters.Add(new FeedFilter
//            {
//                FeedId = 1,
//                UrlHash = "00fa236d36a7ae360c61582390f5edc3"
//            });
//            context.FeedFilters.Add(new FeedFilter
//            {
//                FeedId = 1,
//                UrlHash = "01adfaffdf1211cb1b5981d65bc93eab"
//            });
//            context.FeedFilters.Add(new FeedFilter
//            {
//                FeedId = 1,
//                UrlHash = "e1a79e0b4e951a63f84e9257428abd41"
//            });
//            context.FeedFilters.Add(new FeedFilter
//            {
//                FeedId = 1,
//                UrlHash = "ea75eb648199b19820b1169dbe8b2561"
//            });
//            context.FeedFilters.Add(new FeedFilter
//            {
//                FeedId = 1,
//                UrlHash = "b163e4f141ed9286e7da00c2878f6473"
//            });
//            context.FeedFilters.Add(new FeedFilter
//            {
//                FeedId = 1,
//                UrlHash = "5e047ece1cd1cc19bf6aa6b01a3893e3"
//            });
//            context.FeedFilters.Add(new FeedFilter
//            {
//                FeedId = 1,
//                UrlHash = "0cdb9a4ef88173081464cbb9e8000e86"
//            });
//            context.FeedFilters.Add(new FeedFilter
//            {
//                FeedId = 1,
//                UrlHash = "aac71d089541a02789fe80fc806dbabf"
//            });
//            context.FeedFilters.Add(new FeedFilter
//            {
//                FeedId = 1,
//                UrlHash = "3277518fd12233369747e13dc415de14"
//            });
//            context.FeedFilters.Add(new FeedFilter
//            {
//                FeedId = 1,
//                UrlHash = "cf69b6dd74d44010fdd0eff6d778e70f"
//            });
//            context.FeedFilters.Add(new FeedFilter
//            {
//                FeedId = 1,
//                UrlHash = "cd0802700cef27a775ab057a2ef54aea"
//            });
//        }
//    }
//}
