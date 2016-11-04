using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using BuildAutomation.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Task = System.Threading.Tasks.Task;

namespace BuildAutomation.Controllers
{
    public class BuildController : ApiController
    {

        public void Post()
        {
            HttpResponseMessage response;

            var obj = JsonConvert.DeserializeObject<ReleaseEvent>(Request.Content.ReadAsStringAsync().Result);
            try
            {
                string TelegramAPIKey;
                //get api token from registry
                var key =
                    RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                        .OpenSubKey("SOFTWARE\\Werewolf");
#if DEBUG
                TelegramAPIKey = key.GetValue("DebugAPI").ToString();
#elif RELEASE
            TelegramAPIKey = key.GetValue("ProductionAPI").ToString();
#elif RELEASE2
            TelegramAPIKey = key.GetValue("ProductionAPI2").ToString();
#elif BETA
            TelegramAPIKey = key.GetValue("BetaAPI").ToString();
#endif

                var msg =
                    "Woot!  New build has been released, and is staged on the server.  Do you want me to copy the files and update?";

                var bot = new Telegram.Bot.Client(TelegramAPIKey, System.Environment.CurrentDirectory);
                var result = bot.SendTextMessage(-1001094155678, msg,
                    replyMarkup:
                        new InlineKeyboardMarkup(new[]
                        {new InlineKeyboardButton("Yes", "update|yes"), new InlineKeyboardButton("No", "update|no")})).Result;
                response = new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                string path = HttpContext.Current.Server.MapPath("~/App_Data/error.log");
                StreamWriter writer = new StreamWriter(path);
                writer.WriteLine(e.Message);
            }
            

        }
    }
}
