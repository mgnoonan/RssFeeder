namespace RssFeeder.Mvc.Controllers;

[Authorize]
public class FeedController : Controller
{
    private readonly IFeedDefinitionProvider _feedDefinitionProvider;

    public FeedController(IFeedDefinitionProvider feedDefinitionProvider)
    {
        _feedDefinitionProvider = feedDefinitionProvider;
    }

    // GET: Feed
    public ActionResult Index()
    {
        return View(_feedDefinitionProvider.GetFeeds().OrderBy(i => i.title).AsEnumerable());
    }

    [AllowAnonymous]
    public IActionResult List()
    {
        return Json(_feedDefinitionProvider.GetFeeds());
    }

    // GET: Feed/Details/5
    public ActionResult Details(string id)
    {
        var item = _feedDefinitionProvider.GetFeeds().FirstOrDefault(q => q.id == id);
        return View(item);
    }
}