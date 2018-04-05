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
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;
using Task = System.Threading.Tasks.Task;

namespace BuildAutomation.Controllers
{
    public class BuildController : ApiController
    {
        public long GroupId = -1001077134233;
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
                        //what was released
                        var beta = obj.resource.environment.name.Contains("Beta");
                        var node = obj.resource.environment.name.Contains("Node");



                        var msg = obj.detailedMessage.markdown;
                        InlineKeyboardMarkup menu = null;
                        if (obj.resource.environment.status == "succeeded")
                        {
                            msg += "\nDo you want me to copy the files and update?";
                            menu = new InlineKeyboardMarkup(new[]
                            {
                                new InlineKeyboardCallbackButton("Yes",
                                    $"update|{(beta ? "beta" : "release")}{(node ? "node" : "control")}"),
                                new InlineKeyboardCallbackButton("No", "update|no")
                            });
                        }


                        bot.SendTextMessageAsync(GroupId, msg, replyMarkup: menu, parseMode: ParseMode.Markdown);
                    }

                    if (obj.subscriptionId == buildKey)
                    {
                        var build =
                            JsonConvert.DeserializeObject<BuildEvent>(Request.Content.ReadAsStringAsync().Result);

                        var detail = obj.detailedMessage.markdown;
                        if (detail.Contains("\r\n+ Process 'msbuild.exe'"))
                        {
                            detail = detail.Substring(0, detail.IndexOf("\r\n+ Process 'msbuild.exe'"));
                        }
                        detail = detail.Replace("\r\n+ ", "\r\n");
                        var msg = detail + "\n";
                        var urlPre = "https://github.com/GreyWolfDev/Werewolf/commit/";
                        msg +=
                            $"Built with commit [{build.resource.sourceVersion.Substring(0, 7)}]({urlPre + build.resource.sourceVersion}) as latest";
                        if (build.resource.result == "succeeded")
                            msg += "\nRelease is now being created, you will be notified when it is completed.";

                        bot.SendTextMessageAsync(GroupId, msg, parseMode: ParseMode.Markdown);
                    }
                }
                else
                {
                    //github
                    var push = JsonConvert.DeserializeObject<PushEvent>(body);
                    var msg =
                        $"🔨 <a href='{push.compare}'>{push.commits.Length} new commit{(push.commits.Length > 1 ? "s" : "")} to {push.repository.name}:{push._ref}</a>\n\n";
                    msg = push.commits.Aggregate(msg,
                        (current, a) => current +
                                        $"<a href='{a.url}'>{a.id.Substring(0, 7)}</a>: {a.message} ({a.author.username})\n");




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
                        bot.SendTextMessageAsync(GroupId, msg, parseMode: ParseMode.Html,
                            disableWebPagePreview: true);
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }



                    var none = new InlineKeyboardCallbackButton("No", "build|no");
                    var yes = "Yes";
                    var betaControl = new InlineKeyboardCallbackButton(yes, "build|betacontrol");
                    var betaNode = new InlineKeyboardCallbackButton(yes, "build|betanode");
                    var betaBoth = new InlineKeyboardCallbackButton(yes, "build|betaboth");

                    var releaseControl = new InlineKeyboardCallbackButton(yes, "build|releasecontrol");
                    var releaseNode = new InlineKeyboardCallbackButton(yes, "build|releasenode");
                    var releaseBoth = new InlineKeyboardCallbackButton(yes, "build|releaseboth");
                    InlineKeyboardMarkup menu;

                    msg += "\nThis commit contains changes to ";
                    if (beta)
                    {
                        if (control && node)
                        {
                            menu = new InlineKeyboardMarkup(new[] {betaBoth, none});
                            msg += "Control and Node";
                        }
                        else if (control)
                        {
                            menu = new InlineKeyboardMarkup(new[] {betaControl, none});
                            msg += "Control only";
                        }
                        else
                        {
                            menu = new InlineKeyboardMarkup(new[] {betaNode, none});
                            msg += "Node only";
                        }
                    }
                    else
                    {
                        if (control && node)
                        {
                            menu = new InlineKeyboardMarkup(new[] {releaseBoth, none});
                            msg += "Control and Node";
                        }
                        else if (control)
                        {
                            menu = new InlineKeyboardMarkup(new[] {releaseControl, none});
                            msg += "Control only";
                        }
                        else
                        {
                            menu = new InlineKeyboardMarkup(new[] {releaseNode, none});
                            msg += "Node only";
                        }
                    }
                    msg += $", on {(beta ? "Beta" : "Release")}\n";
                    msg += "Do you want to build?";

                    var r = bot.SendTextMessageAsync(GroupId, msg, replyMarkup: menu, parseMode: ParseMode.Html,
                        disableWebPagePreview: true).Result;


                }

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (AggregateException e)
            {
                var x = e.InnerExceptions[0];
                while (x.InnerException != null)
                    x = x.InnerException;
                bot.SendTextMessageAsync(GroupId, x.Message + "\n" + x.StackTrace);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, x);
            }
            catch (ApiRequestException e)
            {
                var code = e.ErrorCode;
                var x = e.InnerException;
                while (x?.InnerException != null)
                    x = x.InnerException;
                bot.SendTextMessageAsync(GroupId, x?.Message + "\n" + x?.StackTrace);
                return Request.CreateErrorResponse((HttpStatusCode)code, x);
            }
            catch (Exception e)
            {
                //string path = HttpContext.Current.Server.MapPath("~/App_Data/error");
                while (e.InnerException != null)
                    e = e.InnerException;
                bot.SendTextMessageAsync(GroupId, e.Message + "\n" + e.StackTrace);
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
