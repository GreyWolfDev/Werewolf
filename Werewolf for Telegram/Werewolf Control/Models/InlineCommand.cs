using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Database;
using Telegram.Bot.Types;
using Werewolf_Control.Helpers;
using Newtonsoft.Json;

namespace Werewolf_Control.Models
{
    public class InlineCommand
    {
        public string Command { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }

        public InlineCommand(string command, string desc)
        {
            Command = command;
            Description = desc;
        }

        public InlineCommand()
        {
            
        }
    }

    public class StatsInlineCommand : InlineCommand
    {
        public StatsInlineCommand(User u)
        {

            Description = "Get personal stats";
            Command = "stats";
            try
            {
                using (var db = new WWContext())
                {
                    Content = "";
                    //find the player
                    var p = db.Players.FirstOrDefault(x => x.TelegramId == u.Id);
                    if (p == null)
                    {
                        //remove the command
                        Command = "";
                        return;
                    }

                    var gamesPlayed = p.GamePlayers.Count();
                    var won = p.GamePlayers.Count(x => x.Won);
                    var lost = gamesPlayed - won;
                    var survived = p.GamePlayers.Count(x => x.Survived);
                    var roleInfo = db.PlayerRoles(u.Id).ToList();
                    var killed = db.PlayerMostKilled(u.Id).FirstOrDefault();
                    var killedby = db.PlayerMostKilledBy(u.Id).FirstOrDefault();
                    var ach = (Achievements) (p.Achievements ?? 0);
                    var count = ach.GetUniqueFlags().Count();

                    Content = String.IsNullOrWhiteSpace(u.Username)
                        ? $"{u.FirstName.FormatHTML()} the {roleInfo.OrderByDescending(x => x.times).FirstOrDefault()?.role ?? "Noob"}"
                        : $"<a href=\"https://telegram.me/{u.Username}\">{u.FirstName.FormatHTML()} the {roleInfo.OrderByDescending(x => x.times).FirstOrDefault()?.role ?? "Noob"}</a>";
                    Content += $"\n{count.Pad()}Achievements Unlocked!\n" +
                               $"{won.Pad()}Games won ({won*100/gamesPlayed}%)\n" +
                               $"{lost.Pad()}Games lost ({lost*100/gamesPlayed}%)\n" +
                               $"{survived.Pad()}Games survived ({survived*100/gamesPlayed}%)\n" +
                               $"{gamesPlayed.Pad()}Total Games\n" +
                               $"<code>{killed?.times}</code>\ttimes I've gleefully killed {killed?.Name.FormatHTML()}\n" +
                               $"<code>{killedby?.times}</code>\ttimes I've been slaughted by {killedby?.Name.FormatHTML()}\n";

                    var json = p.CustomGifSet;
                    if (!String.IsNullOrEmpty(json))
                    {
                        var data = JsonConvert.DeserializeObject<CustomGifData>(json);
                        if (data.ShowBadge)
                        {
                            if ((p.DonationLevel ?? 0) >= 100)
                                Content += "Donation Level: 🥇";
                            else if ((p.DonationLevel ?? 0) >= 50)
                                Content += "Donation Level: 🥈";
                            else if ((p.DonationLevel ?? 0) >= 10)
                                Content += "Donation Level: 🥉";
                            if (p.Founder ?? false)
                                Content += "\n💎 FOUNDER STATUS! 💎\n<i>(This player donated at least $10USD before there was any reward for donating</i>";
                        }
                    }
                    else
                    {
                        if ((p.DonationLevel ?? 0) >= 100)
                            Content += "Donation Level: 🥇";
                        else if ((p.DonationLevel ?? 0) >= 50)
                            Content += "Donation Level: 🥈";
                        else if ((p.DonationLevel ?? 0) >= 10)
                            Content += "Donation Level: 🥉";

                        if (p.Founder ?? false)
                            Content += "\n💎 FOUNDER STATUS! 💎\n<i>(This player donated at least $10USD before there was any reward for donating</i>";
                    }

                }
            }
            catch (Exception e)
            {
                Content = "Unable to load stats: " + e.Message;
            }
        }
    }

}
