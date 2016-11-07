using System;
using System.Collections.Generic;
using System.Configuration;
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
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Task = System.Threading.Tasks.Task;

namespace BuildAutomation.Controllers
{
    public class BuildController : ApiController
    {

        public void Post()
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<ReleaseEvent>(Request.Content.ReadAsStringAsync().Result);
                var releaseKey = ConfigurationManager.AppSettings.Get("VSTSReleaseId");
                var buildKey = ConfigurationManager.AppSettings.Get("VSTSBuildId");
                if (obj.subscriptionId == releaseKey) //can't have random people triggering this!
                {
                    string TelegramAPIKey = ConfigurationManager.AppSettings.Get("TelegramAPIToken");

                    var msg =
                        "Woot!  New build has been released, and is staged on the server.  Do you want me to copy the files and update?";

                    var bot = new Telegram.Bot.Client(TelegramAPIKey, System.Environment.CurrentDirectory);
                    var result = bot.SendTextMessage(-1001077134233, msg,
                        replyMarkup:
                            new InlineKeyboardMarkup(new[]
                            {new InlineKeyboardButton("Yes", "update|yes"), new InlineKeyboardButton("No", "update|no")}))
                        .Result;
                }

                if (obj.subscriptionId == buildKey)
                {
                    string TelegramAPIKey = ConfigurationManager.AppSettings.Get("TelegramAPIToken");
                    var msg = obj.message.markdown;
                    var bot = new Telegram.Bot.Client(TelegramAPIKey, System.Environment.CurrentDirectory);
                    bot.SendTextMessage(-1001077134233, msg, parseMode: ParseMode.Markdown);
                }
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
