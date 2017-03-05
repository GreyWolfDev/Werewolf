using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Database;
using Telegram.Bot.Types;
using Werewolf_Control.Helpers;

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
            Program.Log("Trying to find stats");
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
                    Program.Log("From User: " + p.Name);
                    var gamesPlayed = p.GamePlayers.Count();
                    Program.Log("Games Played");
                    var won = p.GamePlayers.Count(x => x.Won);
                    Program.Log("Games Won");
                    var lost = gamesPlayed - won;
                    Program.Log("Games Lost");
                    var survived = p.GamePlayers.Count(x => x.Survived);
                    Program.Log("Games Survived");
                    var roleInfo = db.PlayerRoles(u.Id).ToList();
                    Program.Log("roleInfo");
                    var killed = db.PlayerMostKilled(u.Id).FirstOrDefault();
                    Program.Log("Killed");
                    var killedby = db.PlayerMostKilledBy(u.Id).FirstOrDefault();
                    Program.Log("Killed by");
                    var ach = (Achievements) (p.Achievements ?? 0);
                    Program.Log("Achievements");
                    var count = ach.GetUniqueFlags().Count();
                    Program.Log("Achievements Count");

                    Content = String.IsNullOrWhiteSpace(u.Username)
                        ? $"{u.FirstName.FormatHTML()} the {roleInfo.OrderByDescending(x => x.times).FirstOrDefault()?.role ?? "Noob"}"
                        : $"<a href=\"https://telegram.me/{u.Username}\">{u.FirstName.FormatHTML()} the {roleInfo.OrderByDescending(x => x.times).FirstOrDefault()?.role ?? "Noob"}</a>";
                    Content += $"\n{count.Pad()}Achievements Unlocked!\n" +
                               $"{won.Pad()}Games won ({won*100/gamesPlayed}%)\n" +
                               $"{lost.Pad()}Games lost ({lost*100/gamesPlayed}%)\n" +
                               $"{survived.Pad()}Games survived ({survived*100/gamesPlayed}%)\n" +
                               $"{gamesPlayed.Pad()}Total Games\n" +
                               $"<code>{killed?.times}</code>\ttimes I've gleefully killed {killed?.Name.FormatHTML()}\n" +
                               $"<code>{killedby?.times}</code>\ttimes I've been slaughted by {killedby?.Name.FormatHTML()}";
                    Program.Log("Content: " + Content);
                }
            }
            catch (Exception e)
            {
                Program.Log("Fehler: " + e.Message);
                Content = "Unable to load stats: " + e.Message;
            }
        }
    }

}
