using System.Diagnostics;

namespace RssFeeder.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly IFeedDefinitionProvider _feedDefinitionProvider;

    public HomeController(IFeedDefinitionProvider feedDefinitionProvider)
    {
        _feedDefinitionProvider = feedDefinitionProvider;
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        return View(_feedDefinitionProvider.GetFeeds().OrderByDescending(i => i.StatusMessage).ThenBy(i => i.title).AsEnumerable());
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
