using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Helpers;

using System.Web.Http.Results;
using System.Web.Mvc;
using System.Web.Security;
using Newtonsoft.Json;
using Werewolf_Website.Helpers;
using Werewolf_Website.Models;
using Microsoft.AspNet.Identity;

namespace Werewolf_Website.Controllers
{
    [Authorize(Roles = "Developer")]
    [RequireHttps]
    public class AdminApiController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Node(string id)
        {
            ViewBag.NodeId = id;
            return View();
        }

        public ActionResult Game(long groupid, string clientid)
        {
            ViewBag.GroupId = groupid;
            ViewBag.ClientId = clientid;
            return View();
        }
        

        [HttpGet]
        public JsonResult GetStatus()
        {
            return new JsonNetResult {Data= StatusMonitor.GetStatusResponses, JsonRequestBehavior = JsonRequestBehavior.AllowGet};
        }

        [HttpGet]
        public JsonResult GetNodeInfo(string clientid)
        {
            Guid id;
            if (!Guid.TryParse(clientid, out id)) return null;
            //NodeResponseInfo response = null;

            foreach (var bot in StatusMonitor.GetStatusResponses)
            {
                if (bot?.Nodes != null)
                {
                    if (bot.Nodes.Any(n => n.ClientId == id))
                    {
                        return new JsonNetResult {Data =bot.Nodes.FirstOrDefault(x => x.ClientId == id), JsonRequestBehavior = JsonRequestBehavior.AllowGet};
                    }
                }
            }
            //var result =
            //    StatusMonitor.GetStatusResponses.FirstOrDefault(x => x.Nodes != null && x.Nodes.Any(n => n.ClientId == id))?
            //        .Nodes.FirstOrDefault(x => x.ClientId == id);

//#if DEBUG
//            response = new TcpAdminConnection(BotConnectionInfo.DebugIP, BotConnectionInfo.DebugPort).GetNodeInfo(id);
//#endif
//            if (String.IsNullOrEmpty(response?.Version))
//                response = new TcpAdminConnection(BotConnectionInfo.BetaIP, BotConnectionInfo.BetaPort).GetNodeInfo(id);
//            if (String.IsNullOrEmpty(response?.Version))
//                response = new TcpAdminConnection(BotConnectionInfo.Bot1IP, BotConnectionInfo.Bot1Port).GetNodeInfo(id);
//            if (String.IsNullOrEmpty(response?.Version))
//                response = new TcpAdminConnection(BotConnectionInfo.Bot2IP, BotConnectionInfo.Bot2Port).GetNodeInfo(id);


            return null;
            //   return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetGameInfo(long groupid, string clientid)
        {
            GameInfo response = null;
#if DEBUG
            response = new TcpAdminConnection(BotConnectionInfo.DebugIP, BotConnectionInfo.DebugPort).GetGameInfo(groupid, clientid);
#endif
            if (String.IsNullOrEmpty(response?.Language))
                response = new TcpAdminConnection(BotConnectionInfo.BetaIP, BotConnectionInfo.BetaPort).GetGameInfo(groupid, clientid);
            if (String.IsNullOrEmpty(response?.Language))
                response = new TcpAdminConnection(BotConnectionInfo.Bot1IP, BotConnectionInfo.Bot1Port).GetGameInfo(groupid, clientid);
            if (String.IsNullOrEmpty(response?.Language))
                response = new TcpAdminConnection(BotConnectionInfo.Bot2IP, BotConnectionInfo.Bot2Port).GetGameInfo(groupid, clientid);



            return new JsonNetResult(response, JsonRequestBehavior.AllowGet);
        }
    }
}
