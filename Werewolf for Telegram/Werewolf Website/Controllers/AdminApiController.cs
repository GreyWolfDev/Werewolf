using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
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

        public ActionResult Bot(string name)
        {
            ViewBag.BotName = name;
            return View();
        }

        public ActionResult Game(long groupid)
        {
            ViewBag.GroupId = groupid;
            var clientid = (from r in StatusMonitor.GetStatusResponses.Where(x => x != null)
                         from n in r.Nodes
                         from g in n.Games
                         where (g.GroupId == groupid)
                         select n.ClientId).FirstOrDefault();
            if (clientid == Guid.Empty)
                throw new HttpException(404, "Game with Group Id not found!");
            ViewBag.ClientId = clientid.ToString();

            return View();
        }

        public ActionResult Search(string query)
        {
            //try to find the game
            var games = (from r in StatusMonitor.GetStatusResponses.Where(x => x != null)
                from n in r.Nodes
                from g in n.Games
                where (g.GroupId.ToString().Contains(query) || g.GroupName.ToLower().Contains(query.ToLower()))
                         select g);
            ViewBag.Query = query;
            return View(games);
        }
        

        [HttpGet]
        public JsonResult GetStatus()
        {
            return new JsonNetResult {Data= StatusMonitor.GetStatusResponses, JsonRequestBehavior = JsonRequestBehavior.AllowGet};
        }

        [HttpGet]
        public JsonResult GetBotStatus(string botname)
        {
            return new JsonNetResult { Data = StatusMonitor.GetStatusResponses.FirstOrDefault(x => x.BotName == botname), JsonRequestBehavior = JsonRequestBehavior.AllowGet };
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
            var guid = Guid.Parse(clientid);
            //find the node
            var node = StatusMonitor.GetStatusResponses.FirstOrDefault(x => x.Nodes.Any(n => n.ClientId == guid));
            if (node == null)
                return null;
            return new JsonNetResult(new TcpAdminConnection(node.IP, node.Port).GetGameInfo(groupid, clientid), JsonRequestBehavior.AllowGet);
        }

        public void StopNode(string id, string ip, int port)
        {
            //create request
            var request = new StopNodeRequest {ClientId = Guid.Parse(id)};
            new TcpAdminConnection(ip, port).StopNode(request);

        }
    }
}
