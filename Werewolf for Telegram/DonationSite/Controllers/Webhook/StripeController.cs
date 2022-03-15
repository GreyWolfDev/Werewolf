using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Database;
using DonationSite.Models;
using Newtonsoft.Json;
using Stripe;
using Telegram.Bot;

namespace DonationSite.Controllers.Webhook
{
    public class StripeController : ApiController
    {
        public static long LogGroupId = long.Parse(ConfigurationManager.AppSettings.Get("LogGroupId"));
        private static string TelegramAPIKey = ConfigurationManager.AppSettings.Get("TelegramAPIToken");
        private static string XsollaProjectSecretKey = ConfigurationManager.AppSettings.Get("XsollaProjectSecretKey");
        private static string StipeSecret = ConfigurationManager.AppSettings.Get("StripeSecret");
        public static TelegramBotClient bot = new TelegramBotClient(TelegramAPIKey);

        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (var db = new WWContext())
            {
                try
                {
                    var res = Request.Content.ReadAsStringAsync().Result;

                    var stripeEvent = EventUtility.ConstructEvent(res, Request.Headers.GetValues("Stripe-Signature").FirstOrDefault(), StipeSecret);

                    // Handle the event
                    if (stripeEvent.Type == Events.InvoicePaymentSucceeded)
                    {
                        var paymentIntent = stripeEvent.Data.Object as Invoice;
                        var userid = long.Parse(paymentIntent.Number.Substring(0, paymentIntent.Number.IndexOf("-")));
                        var amount = Convert.ToInt32(paymentIntent.Total / 100);
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
                            return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid User ID");
                        }
                        return Request.CreateResponse(HttpStatusCode.OK);
                    }
                    else
                    {
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Unexpected Event Type");
                    }
                }
                catch (StripeException e)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, e.ToString());
                }
            }

        }

    }
}
