using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace RssFeeder.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _log;
    private readonly string _sourceFile = "feeds.json";
    private readonly List<FeedModel> _feeds;

    public HomeController(ILogger<HomeController> logger)
    {
        _log = logger;
        _feeds = System.Text.Json.JsonSerializer.Deserialize<List<FeedModel>>(
            System.IO.File.ReadAllText(_sourceFile),
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        return View(_feeds.OrderBy(i => i.title).AsEnumerable());
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
