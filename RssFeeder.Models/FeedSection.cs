using System.Collections.Generic;

namespace RssFeeder.Models;

public class FeedSection
{
    public string SectionName { get; set; }
    public string ContainerSelector { get; set; }
    public string LinkSelector { get; set; }
    public bool FilterDuplicates { get; set; }
    public string TextSelector { get; set; }
    public string StopHash { get; set; }
    public int? MaxItems { get; set; }
    public bool OnlyIfEmpty { get; set; }
    public List<string> SectionNames { get; set; }
}