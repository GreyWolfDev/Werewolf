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
        public JsonResult GetStatus()
        {
            return Json(new TcpAdminConnection("localhost", DebugPort).GetStatus(), JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetNodeInfo(string clientid)
        {
            var id = Guid.Parse(clientid.ToString());
            return Json(new TcpAdminConnection("localhost", DebugPort).GetNodeInfo(id), JsonRequestBehavior.AllowGet);
        }

        public static int DebugPort = 9059;
        public static int Bot1Port = 9060;
        public static int Bot2Port = 9061;
        public static int BetaPort = 9062;

    }
}
