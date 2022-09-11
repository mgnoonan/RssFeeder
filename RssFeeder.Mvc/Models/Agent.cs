namespace RssFeeder.Mvc.Models;

public record Agent
{
    public string? BrowserAgent { get; set; }
    public string? IpAddress { get; set; }
    public string? Referrer { get; set; }
    public DateTime Timestamp { get; set; }
}
