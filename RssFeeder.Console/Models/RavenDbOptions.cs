namespace RssFeeder.Console.Models;

public class RavenDbOptions
{
    public const string SectionName = "RavenDb";

    public string DatabaseName { get; set; } = "site-parsers";
    public string[] Urls { get; set; } = new[] { "http://127.0.0.1:8080/" };
}