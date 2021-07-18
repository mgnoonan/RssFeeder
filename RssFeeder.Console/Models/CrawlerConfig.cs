using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RssFeeder.Console.Models
{
    public class CrawlerConfig
    {
        public string[] Exclusions { get; set; }
        public string[] VideoHosts { get; set; }
        public string[] IncludeScripts { get; set; }
        public string[] WebDriver { get; set; }
    }
}
