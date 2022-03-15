using Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace DonationSite.Controllers
{
    public class XsollaController : Controller
    {
        // GET: Xsolla
        public async Task<ActionResult> Index(string uid = null, string token = null)
        {
            ViewBag.Title = "Xsolla Donation";
            if (uid != null && long.TryParse(uid, out long userId) && token != null)
            {
                using (var db = new WWContext())
                {
                    var u = db.Players.FirstOrDefault(x => x.TelegramId == userId);
                    ViewBag.CurrentUser = u;
                }
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}