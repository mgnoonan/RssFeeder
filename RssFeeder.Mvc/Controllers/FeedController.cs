using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RssFeeder.Mvc.Models;

namespace RssFeeder.Mvc.Controllers
{
    public class FeedController : Controller
    {
        private IRepository<RssFeederRepository> _repo;
        private readonly string _collectionID = "feeds";

        public FeedController(IRepository<RssFeederRepository> repository)
        {
            _repo = repository;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _repo.Init(_collectionID);
            base.OnActionExecuting(context);
        }

        // GET: Feed
        public async Task<ActionResult> Index()
        {
            var items = await _repo.GetItemsAsync<FeedModel>();

            return View(items.OrderBy(i => i.title));
        }

        // GET: Feed/Details/5
        public async Task<ActionResult> Details(string id)
        {
            var item = await _repo.GetItemAsync<FeedModel>(id);
            return View(item);
        }

        // GET: Feed/Create
        public ActionResult Create()
        {
            // Set some reasonable defaults
            var model = new FeedModel
            {
                id = Guid.NewGuid().ToString()
            };

            return View(model);
        }

        // POST: Feed/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Feed/Edit/5
        public async Task<ActionResult> Edit(string id)
        {
            var item = await _repo.GetItemAsync<FeedModel>(id);
            return View(item);
        }

        // POST: Feed/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Feed/Delete/5
        public async Task<ActionResult> Delete(string id)
        {
            var item = await _repo.GetItemAsync<FeedModel>(id);
            return View(item);
        }

        // POST: Feed/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}