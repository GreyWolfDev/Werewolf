using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Database;

namespace Werewolf_Web.Controllers
{
    [AllowAnonymous]
    public class StatsController : Controller
    {
        //private static werewolfEntities DB = new werewolfEntities();
        // GET: Stats
        public ActionResult Index()
        {
            //Global Stats
            ViewBag.Title = "Werewolf Telegram Stats";
            ViewBag.Description =
                "Werewolf for Telegram. Play werewolf while chatting with your friends on Telegram!\nWerewolf for Telegram Stats";
            return View();
        }

        public ActionResult Group(long id)
        {
            ViewBag.Id = id;
            using (var DB = new WWContext())
            {
                var groupName = DB.Groups.FirstOrDefault(x => x.GroupId == id)?.Name ?? "Invalid Group Id";
                ViewBag.Description = $"Werewolf for Telegram. Stats for {groupName}";
                ViewBag.Title = groupName;
            }
            return View();
        }

        public ActionResult Player(int id)
        {
            ViewBag.Id = id;
            using (var DB = new WWContext())
            {
                var user = DB.Players.FirstOrDefault(x => x.TelegramId == id);
                var userName = user?.Name ?? "Invalid User Id";
                var userImage = user?.ImageFile ?? "noimage.png";
                ViewBag.Description = $"Werewolf for Telegram. Stats for {userName}";
                ViewBag.Title = userName;
                ViewBag.ImagePath = "/Images/" + userImage;
            }
            return View();
        }



        #region Stats

        [HttpGet]
        public JsonResult GlobalStats()
        {
            var statReply = "";
            using (var DB = new WWContext())
            {
                var stat = DB.GlobalStats.First();
                var night1dielink = $"<a href='/Stats/Player/{stat.MostKilledFirstNightId}'>{stat.MostKilledFirstNight}</a>";
                var day1lynchlink = $"<a href='/Stats/Player/{stat.MostLynchedFirstDayId}'>{stat.MostLynchedFirstDay}</a>";
                var day1dielink = $"<a href='/Stats/Player/{stat.MostKilledFirstDayId}'>{stat.MostKilledFirstDay}</a>";
                var survivorlink = $"<a href='/Stats/Player/{stat.BestSurvivorId}'>{stat.BestSurvivor}</a>";
                statReply = "<table class=\"table table-hover\"><tbody>" +
                    $"<tr><td>Games played total</td><td><b>{stat.GamesPlayed}</b></td></tr>" +
                    $"<tr><td>Total player deaths</td><td><b>{stat.PlayersKilled}</b></td></tr>" +
                    $"<tr><td>Total player survivals</td><td><b>{stat.PlayersSurvived}</b></td></tr>" +
                    $"<tr><td>Total players in database</td><td><b>{stat.TotalPlayers}</b></td></tr>" +
                    $"<tr><td>Total groups in database</td><td><b>{stat.TotalGroups}</b></td></tr>" +
                    $"<tr><td>Most likely to die first night</td><td><b>{night1dielink}</td><td>{stat.MostKilledFirstPercent}%</b></td></tr>" +
                    $"<tr><td>Most likely to get lynched first day</td><td><b>{day1lynchlink}</td><td>{stat.MostLynchedFirstPercent}%</b></td></tr>" +
                    $"<tr><td>Most likely to die first 24 hours</td><td><b>{day1dielink}</td><td>{stat.MostKilledFirstDayPercent}%</b></td></tr>" +
                    $"<tr><td>Best survivor</td><td><b>{survivorlink}</td><td>{stat.BestSurvivorPercent}%</b></td></tr>" +
                    $"<tr><td>Last time global stats calculated</td><td><b>{stat.LastRun} (Central US)</b></td></tr>" +
                    "</tbody></table>";

            }
            return Json(statReply, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult DailyCounts()
        {
            using (var db = new WWContext())
            {
                var counts = db.DailyCounts.OrderByDescending(x => x.Day).Take(30).ToList().Select(x => new { Day = x.Day.Day.ToString(), Groups = x.Groups, Games = x.Games, Users = x.Users }).Reverse();
                return Json(counts.ToList(), JsonRequestBehavior.AllowGet);
            }

        }

        [HttpGet]
        public JsonResult GroupStats(long groupid)
        {
            var gStatReply = "";
            using (var DB = new WWContext())
            {
                //var day1LynchInfo = DB.GroupDay1Lynch(groupid).FirstOrDefault();
                //var night1DieInfo = DB.GroupNight1Death(groupid).FirstOrDefault();
                //var day1DieInfo = DB.GroupDay1Death(groupid).FirstOrDefault();
                var surviverInfo = DB.GroupSurvivor(groupid).FirstOrDefault();

                ////most slutty
                //// TODO Add visits to database
                ////most common wolf
                //var commonWolfInfo = (from dbgp in DB.GamePlayers
                //                      join dbg in DB.Games on dbgp.GameId equals dbg.Id
                //                      where dbgp.Role == "Wolf" && dbg.GroupName == groupName
                //                      group dbgp by new { dbgp.PlayerId, dbgp.Role }
                //    into grp
                //                      select new
                //                      {
                //                          name = DB.Players.FirstOrDefault(x => x.Id == grp.Key.PlayerId),
                //                          count = grp.Select(x => x.GameId).Distinct().Count(),
                //                          games = DB.GamePlayers.Count(x => x.PlayerId == grp.Key.PlayerId)
                //                      }).Where(x => x.games >= 10)
                //    .OrderByDescending(x => x.count * 100 / x.games)
                //    .FirstOrDefault();
                ////most common villager
                //var commonVillagerInfo = (from dbgp in DB.GamePlayers
                //                          join dbg in DB.Games on dbgp.GameId equals dbg.Id
                //                          where dbgp.Role == "Villager" && dbg.GroupName == groupName
                //                          group dbgp by new { dbgp.PlayerId, dbgp.Role }
                //    into grp
                //                          select new
                //                          {
                //                              name = DB.Players.FirstOrDefault(x => x.Id == grp.Key.PlayerId),
                //                              count = grp.Select(x => x.GameId).Distinct().Count(),
                //                              games = DB.GamePlayers.Count(x => x.PlayerId == grp.Key.PlayerId)
                //                          }).Where(x => x.games >= 10)
                //    .OrderByDescending(x => x.count * 100 / x.games)
                //    .FirstOrDefault();
                //var timesWolfsWon =
                //    DB.Games.Count(
                //        x =>
                //            new[] { "Wolves", "Wolf" }.Contains(x.Winner) &&
                //            x.GroupName == groupName);
                //var timesVillageWon =
                //    DB.Games.Count(
                //        x => (x.Winner == "Village") && x.GroupName == groupName);
                //var timesTannerWon =
                //    DB.Games.Count(x => x.Winner == "Tanner" && x.GroupName == groupName);
                //now build the reply
                string night1dielink, day1lynchlink, day1dielink, survivorlink;
                //night1dielink = night1DieInfo != null ? $"<a href='../Player/{night1DieInfo.TelegramId}'>{night1DieInfo.Name}</a>" : "Not enough games";
                //day1lynchlink = day1LynchInfo != null ? $"<a href='../Player/{day1LynchInfo.TelegramId}'>{day1LynchInfo.Name}</a>" : "Not enough games";
                //day1dielink = day1DieInfo != null ? $"<a href='../Player/{day1DieInfo.TelegramId}'>{day1DieInfo.Name}</a>" : "Not enough games";
                survivorlink = surviverInfo != null ? $"<a href='../Player/{surviverInfo.TelegramId}'>{surviverInfo.Name}</a>" : "Not enough games";
                gStatReply +=

                    "<table class=\"table table-hover\"><tbody>" +
                    $"<tr><td>Games played total</td><td><b>{DB.Games.Count(x => x.GroupId == groupid)}</b></td></tr>" +
                    // $"<tr><td>Times Wolf / Wolves have won</td><td><b>{timesWolfsWon}</b></td></tr>" +
                    //$"<tr><td>Times Village won</td><td><b>{timesVillageWon}</b></td></tr>" +
                    //$"<tr><td>Times Tanner won</td><td><b>{timesTannerWon}</b></td></tr>" +
                    //$"<tr><td>Most likely to die first night</td><td><b>{night1dielink}</td><td>{(night1DieInfo?.pct ?? 0)}%</b></td></tr>" +
                    //$"<tr><td>Most likely to get lynched first day</td><td><b>{day1lynchlink}</td><td>{(day1LynchInfo?.pct ?? 0)}%</b></td></tr>" +
                    //$"<tr><td>Most likely to die first 24 hours</td><td><b>{day1dielink}</td><td>{(day1DieInfo?.pct ?? 0)}%</b></td></tr>" +
                    $"<tr><td>Best survivor</td><td><b>{survivorlink}</td><td>{surviverInfo?.pct ?? 0}%</b></td></tr>" +
                    //$"<tr><td>Most common villager</td><td><b>{commonVillagerInfo?.name.Name}</td><td>{(commonVillagerInfo?.count ?? 0) * 100 / commonVillagerInfo?.games ?? 1}%</b></td></tr>" +
                    //$"<tr><td>Most common wolf</td><td><b>{commonWolfInfo?.name.Name}</td><td>{(commonWolfInfo?.count ?? 0) * 100 / commonWolfInfo?.games ?? 1}%</b></td></tr>" +

                    "</tbody></table>";
            }
            return Json(gStatReply, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult PlayerStats(int pid)
        {
            using (var DB = new WWContext())
            {
                var p = DB.Players.FirstOrDefault(x => x.TelegramId == pid);
                if (p == null)
                {
                    return Json("", JsonRequestBehavior.AllowGet);
                }
                var gamesPlayed = p.GamePlayers.Count();
                var won = p.GamePlayers.Count(x => x.Won);
                var lost = gamesPlayed - won;
                var survived = p.GamePlayers.Count(x => x.Survived);
                var roleInfo = DB.PlayerRoles(pid).ToList();
                var killed = DB.PlayerMostKilled(pid).FirstOrDefault();
                var killedby = DB.PlayerMostKilledBy(pid).FirstOrDefault();
                var killedlink = killed != null ? $"<a href='../Player/{killed.TelegramId}'>{killed.Name}</a>" : "No kills?";
                var killedbylink = killedby != null ? $"<a href='../Player/{killedby.TelegramId}'>{killedby.Name}</a>" : "Not killed?";
                var reply =
                    "<table class=\"table table-hover\"><tbody>" +
                    $"<tr><td>Games played total</td><td><b>{gamesPlayed}</b></td></tr>" +
                    $"<tr><td>Games won</td><td><b>{won}</td><td>{(won * 100 / gamesPlayed)}%</b></td></tr>" +
                    $"<tr><td>Games lost</td><td><b>{lost}</td><td>{(lost * 100 / gamesPlayed)}%</b></td></tr>" +
                    $"<tr><td>Games survived</td><td><b>{survived}</td><td>{(survived * 100 / gamesPlayed)}%</b></td></tr>" +
                    $"<tr><td>Most common role</td><td><b>{roleInfo.OrderByDescending(x => x.times).FirstOrDefault()?.role ?? "WHAT? YOU HAVEN'T PLAYED?"}</td><td>{roleInfo.OrderByDescending(x => x.times).FirstOrDefault()?.times ?? 0} times</b></td></tr>" +
                    $"<tr><td>Most killed</td><td><b>{killedlink}</td><td>{killed.times} times</b></td></tr>" +
                    $"<tr><td>Most killed by</td><td><b>{killedbylink}</td><td>{killedby.times} times</b></td></tr>" +
                    "</tbody></table>";



                return Json(reply, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult PlayerAchievements(int pid)
        {
            using (var DB = new WWContext())
            {
                var p = DB.Players.FirstOrDefault(x => x.TelegramId == pid);
                if (p == null)
                {
                    return Json("", JsonRequestBehavior.AllowGet);
                }
                if (p.Achievements == null)
                    p.Achievements = 0;
                var ach = (Achievements)p.Achievements;
                var reply = "<br/><table class=\"table table-hover\"><tbody>";
                foreach (var a in ach.GetUniqueFlags())
                    reply += "<tr><td><b>" + a.GetName() + "</b></td><td>" + a.GetDescription() + "</td></tr>";
                reply += "</tbody></table>";
                return Json(reply, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult PlayerImage(int pid)
        {
            using (var db = new WWContext())
            {
                var p = db.Players.FirstOrDefault(x => x.TelegramId == pid);
                if (p == null || String.IsNullOrEmpty(p.ImageFile))
                    return Json("", JsonRequestBehavior.DenyGet);

                return Json("/Images/" + p.ImageFile, JsonRequestBehavior.AllowGet);
            }

        }
        #endregion


    }


}