using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web.Http;
using DonationSite.Models.Xsolla;
using Newtonsoft.Json;
using Telegram.Bot.Exceptions;
using Telegram.Bot;
using Database;
using DonationSite.Models;
using System.Threading.Tasks;

namespace DonationSite.Controllers.Webhook
{
    public class XsollaController : ApiController
    {
        public static long LogGroupId = long.Parse(ConfigurationManager.AppSettings.Get("LogGroupId"));
        private static string TelegramAPIKey = ConfigurationManager.AppSettings.Get("TelegramAPIToken");
        private static string XsollaProjectSecretKey = ConfigurationManager.AppSettings.Get("XsollaProjectSecretKey");
        public static TelegramBotClient bot = new TelegramBotClient(TelegramAPIKey);

        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (var db = new WWContext())
            {
                try
                {
                    var body = Request.Content.ReadAsStringAsync().Result;
                    var header = Request.Headers.GetValues("Authorization").FirstOrDefault();
                    header = header.Replace("Signature ", "");
                    var check = body + XsollaProjectSecretKey;
                    check = SHA1HashStringForUTF8String(check);
                    if (check != header)
                    {
                        return CreateError(Request, "INVALID_SIGNATURE");
                    }

                    var obj = JsonConvert.DeserializeObject<XsollaEvent>(body);
                    switch (obj?.notification_type)
                    {
                        case "user_validation":
                            var userId = obj?.user.id;
                            var id = 0;
                            try
                            {
                                id = int.Parse(userId);
                            }
                            catch
                            {
                                return CreateError(Request, "INVALID_USER");
                            }
                            if (id == 0)
                            {
                                return CreateError(Request, "INVALID_USER");
                            }
                            if (!db.Players.Any(x => x.TelegramId == id))
                            {
                                return CreateError(Request, "INVALID_USER");
                            }
                            return Request.CreateResponse(HttpStatusCode.OK);
                        case "payment":
                            var userid = int.Parse(obj?.user.id);
                            var amount = (int)obj?.purchase?.virtual_currency?.amount;
                            var p = db.Players.FirstOrDefault(x => x.TelegramId == userid);
                            if (p != null)
                            {
                                if (p.DonationLevel == null)
                                    p.DonationLevel = 0;
                                var oldLevel = p.DonationLevel;
                                p.DonationLevel += amount;
                                var level = p.DonationLevel ?? 0;
                                var badge = "";
                                if (level >= 100)
                                    badge += " 🥇";
                                else if (level >= 50)
                                    badge += " 🥈";
                                else if (level >= 10)
                                    badge += " 🥉";
                                if (p.Founder ?? false)
                                    badge += "💎";

                                await bot.SendTextMessageAsync(userid, $"Successfully received ${amount} from you! YAY!\nTotal Donated: ${level}\nCurrent Badge (ingame): {badge}");
                                //check to see how many people have purchased gif packs

                                if (level > 10 && oldLevel < 10)
                                {
                                    p.GifPurchased = true;
                                    CustomGifData data;
                                    var json = p.CustomGifSet;
                                    if (String.IsNullOrEmpty(json))
                                        data = new CustomGifData();
                                    else
                                        data = JsonConvert.DeserializeObject<CustomGifData>(json);
                                    if (!data.HasPurchased)
                                    {
                                        await bot.SendTextMessageAsync(userid,
                                            "Congratulations! You have unlocked Custom Gif Packs :)\nUse /customgif to build your pack, /submitgif to submit for approval");
                                    }
                                    data.HasPurchased = true;

                                    json = JsonConvert.SerializeObject(data);
                                    p.CustomGifSet = json;
                                }
                                db.SaveChanges();
                            }
                            else
                            {
                                return CreateError(Request, "INVALID_USER");
                            }
                            return Request.CreateResponse(HttpStatusCode.OK);
                        case "refund":
                            userid = int.Parse(obj?.user.id);
                            var reason = obj?.refund_details?.reason;
                            amount = (int)obj?.purchase?.total?.amount;
                            await bot.SendTextMessageAsync(userid, $"Your donation did not pass through Xsolla because of: {reason}. If you have any questions please go to @werewolfsupport.");
                            return Request.CreateResponse(HttpStatusCode.OK);
                        default:
                            return Request.CreateResponse(HttpStatusCode.OK);
                    }
                }
                catch (AggregateException e)
                {
                    var x = e.InnerExceptions[0];
                    while (x.InnerException != null)
                        x = x.InnerException;
                    await bot.SendTextMessageAsync(LogGroupId, x.Message + "\n" + x.StackTrace);
                    return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, x);
                }
                catch (ApiRequestException e)
                {
                    var code = e.ErrorCode;
                    var x = e.InnerException;
                    while (x?.InnerException != null)
                        x = x.InnerException;
                    await bot.SendTextMessageAsync(LogGroupId, x?.Message + "\n" + x?.StackTrace);
                    return Request.CreateErrorResponse((HttpStatusCode)code, x);
                }
                catch (Exception e)
                {
                    while (e.InnerException != null)
                        e = e.InnerException;
                    await bot.SendTextMessageAsync(LogGroupId, e.Message + "\n" + e.StackTrace);
                    return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
                }
            }

        }

        public static HttpResponseMessage CreateError(HttpRequestMessage Request, string reason)
        {
            var res = Request.CreateResponse(HttpStatusCode.Forbidden);
            res.Content = new StringContent($"{{\"error\": {{\"code\": \"{reason}\"}}}}");
            return res;
        }

        public static string SHA1HashStringForUTF8String(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);

            var sha1 = SHA1.Create();
            byte[] hashBytes = sha1.ComputeHash(bytes);

            return HexStringFromBytes(hashBytes);
        }

        public static string HexStringFromBytes(byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                var hex = b.ToString("x2");
                sb.Append(hex);
            }
            return sb.ToString();
        }
    }
}
