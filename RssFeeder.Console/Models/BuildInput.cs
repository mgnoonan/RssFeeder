namespace RssFeeder.Console.Models;

public record BuildInput
{
    [Description("The configuration file to build the RSS feeds")]
    public string ConfigFile { get; set; }
}
