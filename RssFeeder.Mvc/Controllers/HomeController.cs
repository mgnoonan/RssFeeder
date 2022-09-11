using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RssFeeder.Mvc.Models;

namespace RssFeeder.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly string _sourceFile = "feeds.json";
    private readonly List<FeedModel> _feeds;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
        _feeds = System.Text.Json.JsonSerializer.Deserialize<List<FeedModel>>(
            System.IO.File.ReadAllText(_sourceFile),
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<FeedModel>();
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        return View(_feeds.OrderBy(i => i.title).AsEnumerable());
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
