using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Database;

namespace Werewolf_Website.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Werewolf Telegram";
            ViewBag.Description = "Werewolf for Telegram. Play werewolf while chatting with your friends on Telegram!";
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Title = "Werewolf Telegram";
            ViewBag.Message = "Information about Werewolf for Telegram";
            ViewBag.Description = "Werewolf for Telegram. Play werewolf while chatting with your friends on Telegram!";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Title = "Werewolf Telegram";
            ViewBag.Message = "Contact information";
            ViewBag.Description = "Werewolf for Telegram. Play werewolf while chatting with your friends on Telegram!";

            return View();
        }

        [HttpGet]
        public JsonResult GetBotStatus()
        {
            return Json(Helpers.StatusMonitor.GetStatus, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult GetPublicGroups()
        {
            using (var DB = new WWContext())
            {
                var pubGroups = (from g in DB.Groups
                    where g.Preferred == true & !String.IsNullOrEmpty(g.UserName)
                    select
                        new
                        {
                            Name = g.Name,
                            Language = g.Language,
                            UserName = g.UserName,
                            Link = "http://telegram.me/" + g.UserName
                        }).ToList();

                //var result = pubGroups.Aggregate("", (current, group) => current + $"<tr><td>{group.Name}</td><td>{group.Language}</td><td>{group.Link}</td></tr>");
                //var table = @"<table class=""table table-hover table-responsive""><thead><tr><th>Name</th><th>Language / Region</th><th>Link</th></tr></thead><tbody>" + result + "</tbody></table>";
                return Json(pubGroups, JsonRequestBehavior.AllowGet);
            }
        }
    }
}