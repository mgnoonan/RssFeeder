using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RssFeeder.Mvc.Models;

namespace RssFeeder.Mvc.Controllers
{
    public class SiteParserController : Controller
    {
        // GET: SiteParser
        [ActionName("Index")]
        public async Task<ActionResult> Index()
        {
            var items = await Repository<SiteParserModel>.GetItemsAsync();
            return View(items.OrderBy(i => i.SiteName));
        }

        // GET: SiteParser/Details/5
        [ActionName("Details")]
        public async Task<ActionResult> Details(string id)
        {
            var item = await Repository<SiteParserModel>.GetItemAsync(id);
            return View(item);
        }

        // GET: SiteParser/Create
        [ActionName("Create")]
        public ActionResult Create()
        {
            return View();
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
                    model.id = model.id.ToLower();
                    model.SiteName = model.SiteName.ToLower();

                    await Repository<SiteParserModel>.CreateItemAsync(model);
                    return RedirectToAction(nameof(Index));
                }

                return View(model);
            }
            catch
            {
                return View();
            }
        }

        // GET: SiteParser/Edit/5
        [ActionName("Edit")]
        public async Task<ActionResult> Edit(string id)
        {
            var item = await Repository<SiteParserModel>.GetItemAsync(id);
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

                    await Repository<SiteParserModel>.UpdateItemAsync(id, model);
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
            var item = await Repository<SiteParserModel>.GetItemAsync(id);
            return View(item);
        }

        // POST: SiteParser/Delete/5
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