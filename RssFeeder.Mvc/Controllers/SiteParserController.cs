using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RssFeeder.Mvc.Models;

namespace RssFeeder.Mvc.Controllers
{
    [Authorize]
    public class SiteParserController : Controller
    {
        private IRepository<RssFeederRepository> _repo;
        private readonly string _collectionID = "site-parsers";

        public SiteParserController(IRepository<RssFeederRepository> repository)
        {
            _repo = repository;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _repo.Init(_collectionID);
            base.OnActionExecuting(context);
        }

        // GET: SiteParser
        [ActionName("Index")]
        public async Task<ActionResult> Index()
        {
            var items = await _repo.GetItemsAsync<SiteParserModel>();

            return View(items.OrderBy(i => i.SiteName));
        }

        // GET: SiteParser/Details/5
        [ActionName("Details")]
        public async Task<ActionResult> Details(string id)
        {
            var item = await _repo.GetItemAsync<SiteParserModel>(id);
            return View(item);
        }

        // GET: SiteParser/Clone
        [ActionName("Clone")]
        public async Task<ActionResult> Clone(string sourceID)
        {
            var item = await _repo.GetItemAsync<SiteParserModel>(sourceID);

            // Set some reasonable defaults
            var model = new SiteParserModel
            {
                id = Guid.NewGuid().ToString(),
                ArticleSelector = item.ArticleSelector,
                ParagraphSelector = item.ParagraphSelector,
                Parser = item.Parser
            };

            return View("Create", model);
        }

        // GET: SiteParser/Create
        [ActionName("Create")]
        public ActionResult Create()
        {
            // Set some reasonable defaults
            var model = new SiteParserModel
            {
                id = Guid.NewGuid().ToString(),
                ArticleSelector = "article",
                ParagraphSelector = "p",
                Parser = "RssFeeder.Console.Parsers.GenericParser"
            };

            return View(model);
        }

        // POST: SiteParser/Create
        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAsync(SiteParserModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Standardize a few properties on lowercase
                    model.id = model.id.Trim().ToLower();
                    model.SiteName = model.SiteName.Trim().ToLower();
                    model.ArticleSelector = model.ArticleSelector.Trim();
                    model.ParagraphSelector = model.ParagraphSelector.Trim();
                    model.Parser = model.Parser.Trim();

                    var items = await _repo.GetItemsAsync<SiteParserModel>(i => i.SiteName == model.SiteName);

                    if (items.Any())
                    {
                        ModelState.AddModelError("duplicate", $"A SiteParser for site name '{model.SiteName}' already exists.");
                        return View(model);
                    }

                    await _repo.CreateItemAsync<SiteParserModel>(model);
                    return RedirectToAction(nameof(Index));
                }

                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("error", ex.Message);
                return View(model);
            }
        }

        // GET: SiteParser/Edit/5
        [ActionName("Edit")]
        public async Task<ActionResult> Edit(string id)
        {
            var item = await _repo.GetItemAsync<SiteParserModel>(id);
            return View(item);
        }

        // POST: SiteParser/Edit/5
        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(string id, SiteParserModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Standardize a few properties on lowercase
                    model.id = model.id.ToLower();
                    model.SiteName = model.SiteName.ToLower();

                    await _repo.UpdateItemAsync<SiteParserModel>(id, model);
                    return RedirectToAction(nameof(Index));
                }

                return View(model);
            }
            catch
            {
                return View();
            }
        }

        // GET: SiteParser/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var item = await _repo.GetItemAsync<SiteParserModel>(id);
            return View(item);
        }

        // POST: SiteParser/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(string id, IFormCollection collection)
        {
            try
            {
                await _repo.DeleteItemAsync(id.ToLower());
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}