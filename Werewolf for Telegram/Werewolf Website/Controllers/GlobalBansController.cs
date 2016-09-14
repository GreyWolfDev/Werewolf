using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Database;

namespace Werewolf_Website.Controllers
{
    [Authorize(Roles = "Developer, GlobalAdmin")]
    [RequireHttps]
    public class GlobalBansController : Controller
    {
        private WWContext db = new WWContext();

        // GET: GlobalBans
        public async Task<ActionResult> Index()
        {
            return View(await db.GlobalBans.ToListAsync());
        }

        // GET: GlobalBans/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GlobalBan globalBan = await db.GlobalBans.FindAsync(id);
            if (globalBan == null)
            {
                return HttpNotFound();
            }
            return View(globalBan);
        }

        // GET: GlobalBans/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: GlobalBans/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,TelegramId,Reason,Expires,BannedBy,BanDate,Name")] GlobalBan globalBan)
        {
            if (ModelState.IsValid)
            {
                db.GlobalBans.Add(globalBan);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(globalBan);
        }

        // GET: GlobalBans/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GlobalBan globalBan = await db.GlobalBans.FindAsync(id);
            if (globalBan == null)
            {
                return HttpNotFound();
            }
            return View(globalBan);
        }

        // POST: GlobalBans/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,TelegramId,Reason,Expires,BannedBy,BanDate,Name")] GlobalBan globalBan)
        {
            if (ModelState.IsValid)
            {
                db.Entry(globalBan).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(globalBan);
        }

        // GET: GlobalBans/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GlobalBan globalBan = await db.GlobalBans.FindAsync(id);
            if (globalBan == null)
            {
                return HttpNotFound();
            }
            return View(globalBan);
        }

        // POST: GlobalBans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            GlobalBan globalBan = await db.GlobalBans.FindAsync(id);
            db.GlobalBans.Remove(globalBan);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
