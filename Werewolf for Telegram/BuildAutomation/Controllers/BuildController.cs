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
using BuildAutomation.Models.Build;
using BuildAutomation.Models.Release;
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

                    var msg = obj.detailedMessage.markdown;
                    InlineKeyboardMarkup menu = null;
                    if (obj.resource.environment.status == "succeeded")
                    {
                        msg += "\nDo you want me to copy the files and update?";
                        menu = new InlineKeyboardMarkup(new[]
                        {
                            new []
                            {
                                new InlineKeyboardButton("Beta - Control", "update|betacontrol"),
                                new InlineKeyboardButton("All - Control", "update|allcontrol"),
                            },
                            new []
                            {
                                new InlineKeyboardButton("Beta - Nodes", "update|betanodes"),
                                new InlineKeyboardButton("All - Nodes", "update|allnodes"),
                            },
                            new []
                            {
                                new InlineKeyboardButton("Beta - Both", "update|betaboth"),
                                new InlineKeyboardButton("All - Both", "update|allboth")
                            },
                            new []
                            {
                                new InlineKeyboardButton("Do Nothing", "update|no"),
                            }
                        });
                    }

                    var bot = new Telegram.Bot.Client(TelegramAPIKey, System.Environment.CurrentDirectory);
                    bot.SendTextMessage(-1001077134233, msg, replyMarkup: menu, parseMode: ParseMode.Markdown);
                }

                if (obj.subscriptionId == buildKey)
                {
                    var build = JsonConvert.DeserializeObject<BuildEvent>(Request.Content.ReadAsStringAsync().Result);
                    string TelegramAPIKey = ConfigurationManager.AppSettings.Get("TelegramAPIToken");
                    var msg = obj.message.markdown + "\n";
                    var urlPre = "https://github.com/parabola949/Werewolf/commit/";
                    msg += $"Build triggered by commit [{build.resource.sourceVersion.Substring(0, 7)}]({urlPre + build.resource.sourceVersion})";
                    if (build.resource.result == "succeeded")
                        msg += "\nRelease is now being created, you will be notified when it is completed.";
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
