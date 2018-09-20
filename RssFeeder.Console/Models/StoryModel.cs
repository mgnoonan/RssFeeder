
public class StoryModel
{
    public string status { get; set; }
    public Article[] articles { get; set; }
}

public class Article
{
    public string storyID { get; set; }
    public string title { get; set; }
    public string subTitle { get; set; }
    public string deckline { get; set; }
    public string teaser { get; set; }
    public string byCredit { get; set; }
    public string author { get; set; }
    public string endStory { get; set; }
    public bool commentsEnabled { get; set; }
    public string commentsCloseDate { get; set; }
    public string displayUpdateDate { get; set; }
    public string rights { get; set; }
    public string pubDate { get; set; }
    public string expirationDate { get; set; }
    public string body { get; set; }
    public string link { get; set; }
    public bool standout { get; set; }
    public string layout { get; set; }
    public string section { get; set; }
    public string subSection { get; set; }
    public Subsections subSections { get; set; }
    public string sectionLabel { get; set; }
    public string sectionSEOKey { get; set; }
    public string subSectionSEOKey { get; set; }
    public string paid { get; set; }
    public string active { get; set; }
    public Image[] images { get; set; }
    public Related related { get; set; }
}

public class Subsections
{
    public string[] news { get; set; }
    public string[] local { get; set; }
    public object[] frontpage { get; set; }
    public object[] breaking { get; set; }
}

public class Related
{
    public object[] links { get; set; }
    public object[] videos { get; set; }
}

public class Image
{
    public string id { get; set; }
    public string title { get; set; }
    public string caption { get; set; }
    public object linkText { get; set; }
    public string photoCredit { get; set; }
    public string orientation { get; set; }
    public string displayOrder { get; set; }
    public string expirationDate { get; set; }
    public string rights { get; set; }
    public string url { get; set; }
    public string url_hero { get; set; }
    public string url_global { get; set; }
    public Cdn cdn { get; set; }
}

public class Cdn
{
    public string[] sizes { get; set; }
    public string host { get; set; }
    public string fileName { get; set; }
}
