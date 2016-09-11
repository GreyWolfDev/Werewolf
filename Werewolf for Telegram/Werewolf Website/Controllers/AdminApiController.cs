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
        private JsonSerializerSettings settings = new JsonSerializerSettings();
        [HttpGet]
        public JsonResult GetStatus()
        {

            

    //get info from each bot
            var response = new List<StatusResponseInfo>
            {
                new TcpAdminConnection(BetaIP, BetaPort).GetStatus(),
                new TcpAdminConnection(Bot1IP, Bot1Port).GetStatus(),
                new TcpAdminConnection(Bot2IP, Bot2Port).GetStatus(),
#if DEBUG
                new TcpAdminConnection(DebugIP, DebugPort).GetStatus()
#endif
            };

            return Json(response, JsonRequestBehavior.AllowGet);

        }

        [HttpGet]
        public JsonResult GetNodeInfo(string clientid)
        {
            var id = Guid.Parse(clientid);
            NodeResponseInfo response = null;
#if DEBUG
            response = new TcpAdminConnection(DebugIP, DebugPort).GetNodeInfo(id);
#endif
            //get each bots infos
            if (String.IsNullOrEmpty(response?.Version))
                response = new TcpAdminConnection(BetaIP, BetaPort).GetNodeInfo(id);
            if (String.IsNullOrEmpty(response?.Version))
                response = new TcpAdminConnection(Bot1IP, Bot1Port).GetNodeInfo(id);
            if (String.IsNullOrEmpty(response?.Version))
                response = new TcpAdminConnection(Bot2IP, Bot2Port).GetNodeInfo(id);

            return Json(response, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetGameInfo(long groupid)
        {
            GameInfo response = null;
#if DEBUG
            response = new TcpAdminConnection(DebugIP, DebugPort).GetGameInfo(groupid);
#endif
            if (String.IsNullOrEmpty(response?.Language))
                response = new TcpAdminConnection(BetaIP, BetaPort).GetGameInfo(groupid);
            if (String.IsNullOrEmpty(response?.Language))
                response = new TcpAdminConnection(Bot1IP, Bot1Port).GetGameInfo(groupid);
            if (String.IsNullOrEmpty(response?.Language))
                response = new TcpAdminConnection(Bot2IP, Bot2Port).GetGameInfo(groupid);

            return Json(response, JsonRequestBehavior.AllowGet);
        }

        //TODO move these settings to registry

        public static int DebugPort = 9059;
        public static int Bot1Port = 9060;
        public static int Bot2Port = 9063;
        public static int BetaPort = 9062;

        public static string DebugIP = "localhost";
        public static string Bot1IP = "138.201.172.151";
        public static string Bot2IP = "138.201.172.151";
        public static string BetaIP = "168.61.40.195"; //beta bot runs on same server as website

    }
}
