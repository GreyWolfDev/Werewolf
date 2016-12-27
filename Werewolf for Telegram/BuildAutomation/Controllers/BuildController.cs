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
        [HttpPost]
        public HttpResponseMessage Post()
        {
            string TelegramAPIKey = ConfigurationManager.AppSettings.Get("TelegramAPIToken");
            var bot = new Telegram.Bot.Client(TelegramAPIKey, System.Environment.CurrentDirectory);
            try
            {
                var body = Request.Content.ReadAsStringAsync().Result;
                var obj = JsonConvert.DeserializeObject<ReleaseEvent>(body);
                if (obj?.subscriptionId != null)
                {
                    var releaseKey = ConfigurationManager.AppSettings.Get("VSTSReleaseId");
                    var buildKey = ConfigurationManager.AppSettings.Get("VSTSBuildId");
                    if (obj.subscriptionId == releaseKey) //can't have random people triggering this!
                    {


                        var msg = obj.detailedMessage.markdown;
                        InlineKeyboardMarkup menu = null;
                        if (obj.resource.environment.status == "succeeded")
                        {
                            msg += "\nDo you want me to copy the files and update?";
                            menu = new InlineKeyboardMarkup(new[]
                            {
                                new[]
                                {
                                    new InlineKeyboardButton("Beta - Control", "update|betacontrol"),
                                    new InlineKeyboardButton("All - Control", "update|allcontrol"),
                                },
                                new[]
                                {
                                    new InlineKeyboardButton("Beta - Nodes", "update|betanodes"),
                                    new InlineKeyboardButton("All - Nodes", "update|allnodes"),
                                },
                                new[]
                                {
                                    new InlineKeyboardButton("Beta - Both", "update|betaboth"),
                                    new InlineKeyboardButton("All - Both", "update|allboth")
                                },
                                new[]
                                {
                                    new InlineKeyboardButton("Do Nothing", "update|no"),
                                }
                            });
                        }

                        
                        bot.SendTextMessage(-1001077134233, msg, replyMarkup: menu, parseMode: ParseMode.Markdown);
                    }

                    if (obj.subscriptionId == buildKey)
                    {
                        var build = JsonConvert.DeserializeObject<BuildEvent>(Request.Content.ReadAsStringAsync().Result);

                        var detail = obj.detailedMessage.markdown;
                        if (detail.Contains("\r\n+ Process 'msbuild.exe'"))
                        {
                            detail = detail.Substring(0, detail.IndexOf("\r\n+ Process 'msbuild.exe'"));
                        }
                        detail = detail.Replace("\r\n+ ", "\r\n");
                        var msg = detail + "\n";
                        var urlPre = "https://github.com/parabola949/Werewolf/commit/";
                        msg +=
                            $"Build triggered by commit [{build.resource.sourceVersion.Substring(0, 7)}]({urlPre + build.resource.sourceVersion})";
                        if (build.resource.result == "succeeded")
                            msg += "\nRelease is now being created, you will be notified when it is completed.";
                        
                        bot.SendTextMessage(-1001077134233, msg, parseMode: ParseMode.Markdown);
                    }
                }
                else
                {
                    //github
                    var push = JsonConvert.DeserializeObject<PushEvent>(body);
                    var msg =
                        $"🔨 <a href='{push.compare}'>{push.commits.Length} new commit{(push.commits.Length > 1 ? "s" : "")} to {push.repository.name}:{push._ref}</a>\n\n";
                    msg = push.commits.Aggregate(msg,
                        (current, a) => current + $"<a href='{a.url}'>{a.id.Substring(0, 7)}</a>: {a.message} ({a.author.username})\n");
                    

                   

                    //string path = HttpContext.Current.Server.MapPath("~/App_Data/github.json");
                    //using (var sw = new StreamWriter(path))
                    //{
                    //    foreach (var c in push.commits)
                    //        sw.WriteLine($"Commit by: {c.committer.username}\nMessage: {c.message}\n");
                    //    sw.WriteLine(body);
                    //}

                    //check what was built
                    var beta = push._ref.Contains("beta"); //beta or master - refs/head/<branch>
                    //now check if control was changed
                    var control =
                        push.commits.Any(
                            x => x.modified.Union(x.added).Union(x.removed).Any(c => c.Contains("Werewolf Control")));
                    var node =
                        push.commits.Any(
                            x => x.modified.Union(x.added).Union(x.removed).Any(c => c.Contains("Werewolf Node")));

                    if (!control && !node) //nothing to build
                    {
                        bot.SendTextMessage(-1001077134233, msg, parseMode: ParseMode.Html, disableWebPagePreview: true);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }



                    var none = new InlineKeyboardButton("No", "build|no");
                    var yes = "Yes";
                    var betaControl = new InlineKeyboardButton(yes, "build|betacontrol");
                    var betaNode = new InlineKeyboardButton(yes, "build|betanode");
                    var betaBoth = new InlineKeyboardButton(yes, "build|betaboth");

                    var releaseControl = new InlineKeyboardButton(yes, "build|releasecontrol");
                    var releaseNode = new InlineKeyboardButton(yes, "build|releasenode");
                    var releaseBoth = new InlineKeyboardButton(yes, "build|releaseboth");
                    InlineKeyboardMarkup menu;

                    msg += "\nThis commit contains changes to ";
                    if (beta)
                    {
                        if (control && node)
                        {
                            menu = new InlineKeyboardMarkup(new[] { betaBoth, none });
                            msg += "Control and Node";
                        }
                        else if (control)
                        {
                            menu = new InlineKeyboardMarkup(new[] { betaControl, none });
                            msg += "Control only";
                        }
                        else
                        {
                            menu = new InlineKeyboardMarkup(new[] { betaNode, none });
                            msg += "Node only";
                        }
                    }
                    else
                    {
                        if (control && node)
                        {
                            menu = new InlineKeyboardMarkup(new[] { releaseBoth, none });
                            msg += "Control and Node";
                        }
                        else if (control)
                        {
                            menu = new InlineKeyboardMarkup(new[] { releaseControl, none });
                            msg += "Control only";
                        }
                        else
                        {
                            menu = new InlineKeyboardMarkup(new[] { releaseNode, none });
                            msg += "Node only";
                        }
                    }
                    msg += $", on {(beta ? "Beta" : "Release")}\n";
                    msg += "Do you want to build?";
                    bot.SendTextMessage(-1001077134233, msg, replyMarkup: menu, parseMode: ParseMode.Html, disableWebPagePreview: true);
                    
                }

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                //string path = HttpContext.Current.Server.MapPath("~/App_Data/error");
                while (e.InnerException != null)
                    e = e.InnerException;
                bot.SendTextMessage(-1001077134233, e.Message + "\n" + e.StackTrace);
                //using (var sw = new StreamWriter(path))
                //{
                //    sw.WriteLine(e.Message);
                //    sw.Flush();
                //}
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }


        }
    }
}
