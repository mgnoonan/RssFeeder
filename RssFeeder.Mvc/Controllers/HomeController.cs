using System.Diagnostics;

namespace RssFeeder.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly string _sourceFile = "feeds.json";
    private readonly List<FeedModel> _feeds;

    public HomeController()
    {
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
