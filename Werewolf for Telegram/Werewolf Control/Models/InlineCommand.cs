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
                                Content += "\n💎 FOUNDER STATUS! 💎\n<i>(This player donated at least $10USD before there was any reward for donating)</i>";
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

    public class KillsInlineCommand : InlineCommand
    {
        public KillsInlineCommand(InlineQuery q)
        {
            User u = q.From;
            Command = "kills";
            try
            {
                using (var db = new WWContext())
                {
                    Content = "";
                    //find the player
                    Player p;
                    p = db.Players.FirstOrDefault(x => x.TelegramId == u.Id);
                    if (p == null)
                    {
                        //remove the command
                        Command = "";
                        return;
                    }
                    Description = $"Gets the players who {p.Name} most killed";

                    var killed = db.PlayerMostKilled(p.TelegramId).AsEnumerable().Take(5);
                    if (p.UserName != null)
                        Content += $"\nPlayers <a href=\"https://telegram.me/{p.UserName}\">{p.Name.FormatHTML()}</a> most killed:\n";
                    else
                        Content += $"\nPlayers {p.Name.FormatHTML()} most killed:\n";
                    foreach (var a in killed)
                    {
                        Content += $"{a.times?.Pad()} {a.Name.ToBold()}\n";
                    }
                }
            }
            catch (Exception e)
            {
                Content = "Unable to load kills: " + e.Message;
            }
        }
    }

    public class KilledByInlineCommand : InlineCommand
    {
        public KilledByInlineCommand(InlineQuery q)
        {
            User u = q.From;
            Command = "killedby";
            try
            {
                using (var db = new WWContext())
                {
                    Content = "";
                    //find the player
                    Player p;
                    p = db.Players.FirstOrDefault(x => x.TelegramId == u.Id);
                    if (p == null)
                    {
                        //remove the command
                        Command = "";
                        return;
                    }
                    Description = $"Get the players who killed {p.Name} the most";

                    var killed = db.PlayerMostKilledBy(p.TelegramId).AsEnumerable().Take(5);
                    if (p.UserName != null)
                        Content += $"\nPlayers who killed <a href=\"https://telegram.me/{p.UserName}\">{p.Name.FormatHTML()}</a> most:\n";
                    else
                        Content += $"\nPlayers who killed {p.Name.FormatHTML()} most:\n";
                    foreach (var a in killed)
                    {
                        Content += $"{a.times?.Pad()} {a.Name.ToBold()}\n";
                    }
                }
            }
            catch (Exception e)
            {
                Content = "Unable to load kills: " + e.Message;
            }
        }
    }

    /*
    public class RolesInlineCommand : InlineCommand
    {
        public RolesInlineCommand(InlineQuery q)
        {
            User u = q.From;
            Command = "roles";
            try
            {
                using (var db = new WWContext())
                {
                    Content = "";
                    //find the player
                    Player p;
                    p = db.Players.FirstOrDefault(x => x.TelegramId == u.Id);
                    if (p == null)
                    {
                        //remove the command
                        Command = "";
                        return;
                    }
                    Description = $"Get the most played role of {p.Name}";

                    var totalRoles = db.PlayerRoles(p.TelegramId).Sum(x => x.times);
                    var roleInfo = db.PlayerRoles(p.TelegramId).ToList().OrderByDescending(x => x.times).Take(5);
                    Content += $"\nPapéis que <a href=\"https://telegram.me/{p.UserName}\">{p.Name.FormatHTML()}</a> mais jogou:\n";
                    foreach (var a in roleInfo)
                    {
                        var role = Commands.GetLocaleString(a.role, p.Language);
                        Content += $"<code>{a.times.ToString().PadLeft(3)} ({(((double)a.times / (double)totalRoles) * 100.0).ToString("#0.0").PadLeft(4)}%)</code> {role.ToBold()}\n";
                    }
                }
            }
            catch (Exception e)
            {
                Content = "Unable to load roles: " + e.Message;
            }
        }
    }
    */
    
    public class TypesOfDeathInlineCommand : InlineCommand
    {
        public TypesOfDeathInlineCommand(InlineQuery q)
        {
            User u = q.From;
            Command = "deaths";
            try
            {
                using (var db = new WWContext())
                {
                    Content = "";
                    //find the player
                    Player p;
                    
                    p = db.Players.FirstOrDefault(x => x.TelegramId == u.Id);
                    if (p == null)
                    {
                        //remove the command
                        Command = "";
                        return;
                    }
                    
                    Description = $"Get the types of death that {p.Name} most had";

                    var deaths = (from gk in db.GameKills
                                  join pla in db.Players on gk.VictimId equals pla.Id
                                  where pla.TelegramId == p.TelegramId
                                  where gk.KillMethodId != 0
                                  group gk by gk.KillMethodId);
                    var totalDeaths = deaths.Sum(x => x.Count());
                    var deathInfo = deaths.OrderByDescending(x => x.Count()).Take(5);

                    if (p.UserName != null)
                        Content += $"\nTypes of deaths that <a href=\"https://telegram.me/{p.UserName}\">{p.Name.FormatHTML()}</a> most had:\n";
                    else
                        Content += $"\nTypes of deaths that {p.Name.FormatHTML()} most had:\n";
                    foreach (var a in deathInfo)
                    {
                        var killMethod = Enum.GetName(typeof(KillMthd), a.Key);
                        Content += $"<code>{a.Count().ToString().PadLeft(4)} ({((double)(a.Count() / (double)totalDeaths) * 100.0).ToString("#0.0").PadLeft(4)}%)</code> {killMethod.ToBold()}\n";
                    }
                }
            }
            catch (Exception e)
            {
                Content = "Unable to load kill methods: " + e.Message;
            }
        }
    }

}
