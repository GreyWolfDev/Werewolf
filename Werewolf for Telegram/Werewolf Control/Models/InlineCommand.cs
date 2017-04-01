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

            Description = "Obtem estatísticas pessoais";
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
            Description = "Obtem os jogadores que você mais matou";
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
                    Content += $"\nJogadores que eu mais matei:\n";
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
            Description = "Obtem os jogadores que mais te mataram";
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
                    Content += $"\nJogadores que mais me mataram:\n";
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

    public class RolesInlineCommand : InlineCommand
    {
        public RolesInlineCommand(User u)
        {
            Description = "Obtem os papéis que você mais jogou";
            Command = "roles";
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

                    var totalRoles = db.PlayerRoles(u.Id).Sum(x => x.times);
                    var roleInfo = db.PlayerRoles(u.Id).ToList().OrderByDescending(x => x.times).Take(5);
                    Content += $"\nPapéis que eu mais joguei:\n";
                    foreach (var a in roleInfo)
                    {
                        var role = Commands.GetLocaleString(a.role, p.Language);
                        Content += $"<code>{a.times.ToString().PadLeft(3)} ({(((double)a.times/(double)totalRoles)*100.0).ToString("#0.0").PadLeft(4)}%)</code> {role.ToBold()}\n";
                    }
                }
            }
            catch (Exception e)
            {
                Content = "Unable to load roles: " + e.Message;
            }
        }
    }

    public class TypesOfDeathInlineCommand : InlineCommand
    {
        public TypesOfDeathInlineCommand(User u)
        {
            Description = "Obtem os tipos de morte que você mais teve";
            Command = "deaths";
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
                    var deaths = (from gk in db.GameKills
                                  join pla in db.Players on gk.VictimId equals pla.Id
                                  where pla.TelegramId == p.TelegramId
                                  where gk.KillMethodId != 0
                                  group gk by gk.KillMethodId);
                    var totalDeaths = deaths.Sum(x => x.Count());
                    var deathInfo = deaths.OrderByDescending(x => x.Count()).Take(5);

                    Content += $"\nTipos de mortes que eu mais tive:\n";
                    foreach (var a in deathInfo)
                    {
                        var killMethod = Enum.GetName(typeof(KillMthd), a.Key);
                        Content += $"<code>{a.Count().ToString().PadLeft(4)} ({((double)(a.Count()/(double)totalDeaths)*100.0).ToString("#0.0").PadLeft(4)}%)</code> {killMethod.ToBold()}\n";
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
