namespace RssFeeder.Console.Exporters;

public class JsonLdRumbleValues
{
    public string context { get; set; }
    public string type { get; set; }
    public string name { get; set; }
    public string playerType { get; set; }
    public string description { get; set; }
    public string thumbnailUrl { get; set; }
    public DateTime uploadDate { get; set; }
    public string duration { get; set; }
    public string embedUrl { get; set; }
    public string url { get; set; }
    public Interactionstatistic interactionStatistic { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public string videoQuality { get; set; }
    public Potentialaction potentialAction { get; set; }
    public string logo { get; set; }
    public string[] sameAs { get; set; }
}

public class Interactionstatistic
{
    public string type { get; set; }
    public Interactiontype interactionType { get; set; }
    public int userInteractionCount { get; set; }
}

public class Interactiontype
{
    public string type { get; set; }
}

public class Potentialaction
{
    public string type { get; set; }
    public string target { get; set; }
    public string queryinput { get; set; }
}
