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
                    var ach = (Achievements)(p.Achievements ?? 0);
                    var count = ach.GetUniqueFlags().Count();

                    Content = String.IsNullOrWhiteSpace(u.Username)
                        ? $"{u.FirstName.FormatHTML()}, {Commands.GetLocaleString(roleInfo.OrderByDescending(x => x.times).FirstOrDefault()?.role, p.Language) ?? "Noob"}"
                        : $"<a href=\"https://telegram.me/{u.Username}\">{u.FirstName.FormatHTML()}, {Commands.GetLocaleString(roleInfo.OrderByDescending(x => x.times).FirstOrDefault()?.role, p.Language) ?? "Noob"}</a>";
                    Content += $"\n{count.Pad()} {Commands.GetLocaleString("AchievementsUnlocked", p.Language)}\n" +
                               $"{won.Pad()} {Commands.GetLocaleString("GamesWon", p.Language)} ({won * 100 / gamesPlayed}%)\n" +
                               $"{lost.Pad()} {Commands.GetLocaleString("GamesLost", p.Language)} ({lost * 100 / gamesPlayed}%)\n" +
                               $"{survived.Pad()} {Commands.GetLocaleString("GamesSurvived", p.Language)} ({survived * 100 / gamesPlayed}%)\n" +
                               $"{gamesPlayed.Pad()} {Commands.GetLocaleString("TotalGames", p.Language)} \n" +
                               $"<code>{killed?.times}</code>\t{Commands.GetLocaleString("TimesKilled", p.Language)} {killed?.Name.FormatHTML()}\n" +
                               $"<code>{killedby?.times}</code>\t{Commands.GetLocaleString("TimesKilledBy", p.Language)} {killedby?.Name.FormatHTML()}";
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
        public KillsInlineCommand(User u)
        {
            Description = "Obtem lista dos 5 jogadores que você mais matou";
            Command = "kills";
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

                    var killed = db.PlayerMostKilled(u.Id).AsEnumerable();
                    Content += $"\nTop 5 Jogadores que eu mais matei:\n";
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
        public KilledByInlineCommand(User u)
        {
            Description = "Obtem lista dos 5 jogadores que mais te mataram";
            Command = "killedby";
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

                    var killed = db.PlayerMostKilledBy(u.Id).AsEnumerable();
                    Content += $"\nTop 5 Jogadores que mais me mataram:\n";
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
}
