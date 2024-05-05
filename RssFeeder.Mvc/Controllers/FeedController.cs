namespace RssFeeder.Mvc.Controllers;

[Authorize]
public class FeedController : Controller
{
    private readonly string _sourceFile = "feeds.json";
    private readonly List<FeedModel> _feeds;

    public FeedController()
    {
        _feeds = System.Text.Json.JsonSerializer.Deserialize<List<FeedModel>>(
            System.IO.File.ReadAllText(_sourceFile),
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    // GET: Feed
    public ActionResult Index()
    {
        return View(_feeds.OrderBy(i => i.title).AsEnumerable());
    }

    [AllowAnonymous]
    public IActionResult List()
    {
        return Json(_feeds);
    }

    // GET: Feed/Details/5
    public ActionResult Details(string id)
    {
        var item = _feeds.Find(q => q.id == id);
        return View(item);
    }
}