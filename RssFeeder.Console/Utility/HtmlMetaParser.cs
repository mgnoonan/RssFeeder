using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using System.Web.UI.HtmlControls;

namespace RssFeeder.Console.Utility
{
    /// <summary>
    /// http://www.codeproject.com/KB/recipes/MetaTagParser.aspx
    /// </summary>
    public class HtmlMetaParser
    {
        public enum RobotHtmlMeta
        {
            None = 0, NoIndex, NoFollow,
            NoIndexNoFollow
        };

        public static List<HtmlMeta> Parse(string htmldata)
        {
            Regex metaregex =
                new Regex(@"<meta\s*(?:(?:\b(\w|-)+\b\s*(?:=\s*(?:""[^""]*""|'" +
                          @"[^']*'|[^""'<> ]+)\s*)?)*)/?\s*>",
                          RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

            List<HtmlMeta> MetaList = new List<HtmlMeta>();
            foreach (Match metamatch in metaregex.Matches(htmldata))
            {
                HtmlMeta mymeta = new HtmlMeta();

                Regex submetaregex =
                    new Regex(@"(?<name>\b(\w|-)+\b)\" +
                              @"s*=\s*(""(?<value>" +
                              @"[^""]*)""|'(?<value>[^']*)'" +
                              @"|(?<value>[^""'<> ]+)\s*)+",
                              RegexOptions.IgnoreCase |
                              RegexOptions.ExplicitCapture);

                foreach (Match submetamatch in
                         submetaregex.Matches(metamatch.Value.ToString()))
                {
                    if ("http-equiv" ==
                          submetamatch.Groups["name"].ToString().ToLower())
                        mymeta.HttpEquiv =
                          submetamatch.Groups["value"].ToString();

                    if (("name" ==
                         submetamatch.Groups["name"].ToString().ToLower())
                         && (mymeta.HttpEquiv == String.Empty))
                        mymeta.Name = submetamatch.Groups["value"].ToString();

                    if ("scheme" ==
                        submetamatch.Groups["name"].ToString().ToLower())
                        mymeta.Scheme = submetamatch.Groups["value"].ToString();

                    if ("content" ==
                        submetamatch.Groups["name"].ToString().ToLower())
                    {
                        mymeta.Content = submetamatch.Groups["value"].ToString();
                        MetaList.Add(mymeta);
                    }
                }
            }

            return MetaList;
        }

        public static RobotHtmlMeta ParseRobotMetaTags(string htmldata)
        {
            List<HtmlMeta> MetaList = HtmlMetaParser.Parse(htmldata);

            RobotHtmlMeta result = RobotHtmlMeta.None;
            foreach (HtmlMeta meta in MetaList)
            {
                if (meta.Name.ToLower().IndexOf("robots") != -1 ||
                        meta.Name.ToLower().IndexOf("robot") != -1)
                {
                    string content = meta.Content.ToLower();
                    if (content.IndexOf("noindex") != -1 &&
                        content.IndexOf("nofollow") != -1)
                    {
                        result = RobotHtmlMeta.NoIndexNoFollow;
                        break;
                    }
                    if (content.IndexOf("noindex") != -1)
                    {
                        result = RobotHtmlMeta.NoIndex;
                        break;
                    }
                    if (content.IndexOf("nofollow") != -1)
                    {
                        result = RobotHtmlMeta.NoFollow;
                        break;
                    }
                }
            }

            return result;
        }
    }
}