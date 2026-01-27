namespace RssFeeder.Console.Models;

public record AuditInput
{
    [Description("The configuration file to build the RSS feeds")]
    public string ConfigFile { get; set; }
}
