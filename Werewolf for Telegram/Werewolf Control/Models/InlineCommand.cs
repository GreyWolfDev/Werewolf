using System;
using System.Linq;
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

        public InlineCommand(string command, string desc, string content)
        {
            Command = command;
            Description = desc;
            Content = content;
        }

        public InlineCommand()
        {
            
        }
    }

    public class StatsInlineCommand : InlineCommand
    {
        public StatsInlineCommand(InlineQuery q)
        {
            User u = q.From;
            Description = "Obtem estatísticas pessoais\n(Dica: Experimente digitar um @username para consultar seus stats!)";
            Command = "stats";
            try
            {
                using (var db = new WWContext())
                {
                    Content = "";
                    //find the player
                    Player p;
                    if (String.IsNullOrWhiteSpace(q.Query))
                    {
                        p = db.Players.FirstOrDefault(x => x.TelegramId == u.Id);
                        if (p == null)
                        {
                            //remove the command
                            Command = "";
                            return;
                        }
                    }
                    else
                    {
                        p = db.Players.FirstOrDefault(x => x.UserName.Equals(q.Query.Substring(1).Trim(), StringComparison.CurrentCultureIgnoreCase));
                        if (p == null)
                        {
                            Description = $"Nenhum jogador encontrado com username {q.Query} para mostrar seus {Command}";
                            Content = $"Nenhum jogador encontrado com username {q.Query} para mostrar seus {Command}";
                            return;
                        }
                        Description = $"Obtem estatísticas do jogador {p.Name}";
                        Command += $" ({p.Name})";
                    }

                    var gamesPlayed = p.GamePlayers.Count();
                    var won = p.GamePlayers.Count(x => x.Won);
                    var lost = gamesPlayed - won;
                    var survived = p.GamePlayers.Count(x => x.Survived);
                    var roleInfo = db.PlayerRoles(p.TelegramId).ToList();
                    var killed = db.PlayerMostKilled(p.TelegramId).FirstOrDefault();
                    var killedby = db.PlayerMostKilledBy(p.TelegramId).FirstOrDefault();
                    var ach = (Achievements)(p.Achievements ?? 0);
                    var count = ach.GetUniqueFlags().Count();
                    var totalAch = Enum.GetValues(typeof(Achievements)).Length;

                    Content = String.IsNullOrWhiteSpace(p.UserName)
                        ? $"{p.Name.FormatHTML()}, {Commands.GetLocaleString(roleInfo.OrderByDescending(x => x.times).FirstOrDefault()?.role, p.Language) ?? "Noob"}"
                        : $"<a href=\"https://telegram.me/{p.UserName}\">{p.Name.FormatHTML()}, {Commands.GetLocaleString(roleInfo.OrderByDescending(x => x.times).FirstOrDefault()?.role, p.Language) ?? "Noob"}</a>";
                    Content += $"\n<code>{(count+"/"+ totalAch).PadRight(5)}</code> {Commands.GetLocaleString("AchievementsUnlocked", p.Language)}\n" +
                               $"{won.Pad()} {Commands.GetLocaleString("GamesWon", p.Language)} ({(((double)won / (double)gamesPlayed) * 100.0).ToString("#0.0")}%)\n" +
                               $"{lost.Pad()} {Commands.GetLocaleString("GamesLost", p.Language)} ({(((double)lost / (double)gamesPlayed) * 100.0).ToString("#0.0")}%)\n" +
                               $"{survived.Pad()} {Commands.GetLocaleString("GamesSurvived", p.Language)} ({(((double)survived / (double)gamesPlayed) * 100.0).ToString("#0.0")}%)\n" +
                               $"{gamesPlayed.Pad()} {Commands.GetLocaleString("TotalGames", p.Language)} \n" +
                               $"<code>{killed?.times}</code>\t{Commands.GetLocaleString("TimesKilled", p.Language)} {killed?.Name.FormatHTML()}\n" +
                               $"<code>{killedby?.times}</code>\t{Commands.GetLocaleString("TimesKilledBy", p.Language)} {killedby?.Name.FormatHTML()}";
			
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
                    if (String.IsNullOrWhiteSpace(q.Query))
                    {
                        p = db.Players.FirstOrDefault(x => x.TelegramId == u.Id);
                        if (p == null)
                        {
                            //remove the command
                            Command = "";
                            return;
                        }
                    }
                    else
                    {
                        p = db.Players.FirstOrDefault(x => x.UserName.Equals(q.Query.Substring(1).Trim(), StringComparison.CurrentCultureIgnoreCase));
                        if (p == null)
                        {
                            Description = $"Nenhum jogador encontrado com username {q.Query} para mostrar seus {Command}";
                            Content = $"Nenhum jogador encontrado com username {q.Query} para mostrar seus {Command}";
                            return;
                        }
                        Command += $" ({p.Name})";
                    }
                    Description = $"Obtem os jogadores que {p.Name} mais matou";

                    var killed = db.PlayerMostKilled(p.TelegramId).AsEnumerable();
                    Content += $"\nJogadores que <a href=\"https://telegram.me/{p.UserName}\">{p.Name.FormatHTML()}</a> mais matou:\n";
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
                    if (String.IsNullOrWhiteSpace(q.Query))
                    {
                        p = db.Players.FirstOrDefault(x => x.TelegramId == u.Id);
                        if (p == null)
                        {
                            //remove the command
                            Command = "";
                            return;
                        }
                    }
                    else
                    {
                        p = db.Players.FirstOrDefault(x => x.UserName.Equals(q.Query.Substring(1).Trim(), StringComparison.CurrentCultureIgnoreCase));
                        if (p == null)
                        {
                            Description = $"Nenhum jogador encontrado com username {q.Query} para mostrar seus {Command}";
                            Content = $"Nenhum jogador encontrado com username {q.Query} para mostrar seus {Command}";
                            return;
                        }
                        Command += $" ({p.Name})";
                    }
                    Description = $"Obtem os jogadores que mais mataram {p.Name}";

                    var killed = db.PlayerMostKilledBy(p.TelegramId).AsEnumerable();
                    Content += $"\nJogadores que mais mataram <a href=\"https://telegram.me/{p.UserName}\">{p.Name.FormatHTML()}</a>:\n";
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
                    if (String.IsNullOrWhiteSpace(q.Query))
                    {
                        p = db.Players.FirstOrDefault(x => x.TelegramId == u.Id);
                        if (p == null)
                        {
                            //remove the command
                            Command = "";
                            return;
                        }
                    }
                    else
                    {
                        p = db.Players.FirstOrDefault(x => x.UserName.Equals(q.Query.Substring(1).Trim(), StringComparison.CurrentCultureIgnoreCase));
                        if (p == null)
                        {
                            Description = $"Nenhum jogador encontrado com username {q.Query} para mostrar seus {Command}";
                            Content = $"Nenhum jogador encontrado com username {q.Query} para mostrar seus {Command}";
                            return;
                        }
                        Command += $" ({p.Name})";
                    }
                    Description = $"Obtem os papéis que {p.Name} mais jogou";

                    var totalRoles = db.PlayerRoles(p.TelegramId).Sum(x => x.times);
                    var roleInfo = db.PlayerRoles(p.TelegramId).ToList().OrderByDescending(x => x.times).Take(5);
                    Content += $"\nPapéis que <a href=\"https://telegram.me/{p.UserName}\">{p.Name.FormatHTML()}</a> mais jogou:\n";
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
                    if (String.IsNullOrWhiteSpace(q.Query))
                    {
                        p = db.Players.FirstOrDefault(x => x.TelegramId == u.Id);
                        if (p == null)
                        {
                            //remove the command
                            Command = "";
                            return;
                        }
                    }
                    else
                    {
                        p = db.Players.FirstOrDefault(x => x.UserName.Equals(q.Query.Substring(1).Trim(), StringComparison.CurrentCultureIgnoreCase));
                        if (p == null)
                        {
                            Description = $"Nenhum jogador encontrado com username {q.Query} para mostrar seus {Command}";
                            Content = $"Nenhum jogador encontrado com username {q.Query} para mostrar seus {Command}";
                            return;
                        }
                        Command += $" ({p.Name})";
                    }
                    Description = $"Obtem os tipos de morte que {p.Name} mais teve";

                    var deaths = (from gk in db.GameKills
                                  join pla in db.Players on gk.VictimId equals pla.Id
                                  where pla.TelegramId == p.TelegramId
                                  where gk.KillMethodId != 0
                                  group gk by gk.KillMethodId);
                    var totalDeaths = deaths.Sum(x => x.Count());
                    var deathInfo = deaths.OrderByDescending(x => x.Count()).Take(5);

                    Content += $"\nTipos de mortes que <a href=\"https://telegram.me/{p.UserName}\">{p.Name.FormatHTML()}</a> mais teve:\n";
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
