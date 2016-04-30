using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Threading.Tasks;
using RssFeeder.Models;

namespace RssFeeder.Controllers
{
    public class FeedsController : Controller
    {
        // GET: Feed
        public ActionResult Index()
        {
            var feeds = DocumentDBRepository<Feed>.GetFeeds();
            return View(feeds);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "ID,Title,Url,Description,Language,CustomParser")] Feed feed)
        {
            if (ModelState.IsValid)
            {
                await DocumentDBRepository<Feed>.CreateItemAsync(feed);
                return RedirectToAction("Index");
            }
            return View(feed);
        }

        public ActionResult Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Feed item = (Feed)DocumentDBRepository<Feed>.GetItem(d => d.ID == id);

            if (item == null)
            {
                return HttpNotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "ID,Title,Url,Description,Language,CustomParser")] Feed item)
        {
            if (ModelState.IsValid)
            {
                await DocumentDBRepository<Feed>.UpdateItemAsync(item.ID, item);
                return RedirectToAction("Index");
            }

            return View(item);
        }
    }
}