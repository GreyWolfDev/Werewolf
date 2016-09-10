using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Database;

namespace StatsRotation
{
    class Program
    {
        static void Main(string[] args)
        {
            new Thread(StatThread).Start();
            Thread.Sleep(-1);
        }

        static void StatThread()
        {
            while (true)
            {
                try
                {
                    //start with global stats
                    GlobalStats();
                    Thread.Sleep(TimeSpan.FromHours(1));


                    ////now groups
                    //List<long> groupids;
                    //using (var db = new WWContext())
                    //{
                    //    groupids = db.Groups.Select(x => x.GroupId).ToList();
                    //}
                    //var start = DateTime.Now;
                    //foreach (var g in groupids)
                    //{
                    //    var index = groupids.IndexOf(g);
                    //    Console.Clear();
                    //    Console.WriteLine($"Working on group {groupids.IndexOf(g)} out of {groupids.Count}");
                    //    if (index != 0)
                    //    {
                    //        var time = (DateTime.Now - start).Ticks;
                    //        Console.WriteLine($"Time taken so far: {new TimeSpan(time)}");
                    //        time /= index;
                    //        time *= groupids.Count;
                    //        Console.WriteLine($"Total time estimated: {new TimeSpan(time)}");
                    //        time -= (DateTime.Now - start).Ticks;
                    //        Console.WriteLine($"Estimated time remaining: {new TimeSpan(time)}");
                    //    }
                    //    //calculate time
                    //    GroupStats(g);
                    //    //Thread.Sleep(500);
                    //}
                    ////players
                    //List<int> playerids;
                    //using (var db = new WWContext())
                    //{
                    //    playerids = db.Players.Select(x => x.Id).ToList();
                    //}
                    //foreach (var p in playerids)
                    //{
                    //    PlayerStats(p);
                    //    //Thread.Sleep(500);
                    //}
                }
                catch (Exception ex)
                {

                }
            }
        }

        static void GlobalStats()
        {
            Console.WriteLine("Calculating Global Stats");
            try
            {
                //now do daily counts
                using (var db = new WWContext())
                {
                    Console.WriteLine("Updating Daily Counts");
                    //get daily counts
                    var counts = db.getDailyCounts();
                    db.DailyCounts.RemoveRange(db.DailyCounts);
                    foreach (var count in counts)
                        db.DailyCounts.Add(new DailyCount { Day = count.Day.Value, Games = count.Games.Value, Groups = count.Groups.Value, Users = count.players.Value });
                    db.SaveChanges();
                    Console.WriteLine("Done");

                }

                using (var DB = new WWContext())
                {
                    DB.Database.CommandTimeout = 600;
                    Console.WriteLine("Total games..");
                    var gamesPlayed = DB.Games.Count();
                    Console.WriteLine("Night 1 Death");
                    var night1death = DB.GlobalNight1Death().First();
                    Console.WriteLine("Day 1 Lynch");
                    var day1lynch = DB.GlobalDay1Lynch().First();
                    Console.WriteLine("Day 1 Death");
                    var day1death = DB.GlobalDay1Death().First();
                    Console.WriteLine("Survivor");
                    var survivor = DB.GlobalSurvivor().First();
                    Console.WriteLine("Creating stat object");
                    var stat = DB.GlobalStats.FirstOrDefault();
                    if (stat == null)
                    {
                        stat = DB.GlobalStats.Create();
                        DB.GlobalStats.Add(stat);
                    }

                    stat.PlayersKilled = DB.GamePlayers.Count(x => !x.Survived);
                    stat.PlayersSurvived = DB.GamePlayers.Count(x => x.Survived);
                    stat.TotalGroups = DB.Groups.Count();
                    stat.TotalPlayers = DB.Players.Count();
                    stat.BestSurvivor = survivor.Name;
                    stat.BestSurvivorPercent = (int)survivor.pct;
                    stat.BestSurvivorId = survivor.TelegramId;
                    stat.GamesPlayed = gamesPlayed;
                    stat.LastRun = DateTime.Now;
                    stat.MostKilledFirstDay = day1death.Name;
                    stat.MostKilledFirstDayPercent = day1death.pct;
                    stat.MostKilledFirstDayId = day1death.TelegramId;
                    stat.MostKilledFirstNight = night1death.Name;
                    stat.MostKilledFirstPercent = night1death.pct;
                    stat.MostKilledFirstNightId = night1death.TelegramId;
                    stat.MostLynchedFirstDay = day1lynch.Name;
                    stat.MostLynchedFirstPercent = day1lynch.pct;
                    stat.MostLynchedFirstDayId = day1lynch.TelegramId;
                    Console.WriteLine("Saving to database");
                    DB.SaveChanges();
                    Console.WriteLine("Done");
                }

                //now do daily counts
                using (var db = new WWContext())
                {
                    Console.WriteLine("Updating Daily Counts");
                    //get daily counts
                    var counts = db.getDailyCounts();
                    db.DailyCounts.RemoveRange(db.DailyCounts);
                    foreach (var count in counts)
                        db.DailyCounts.Add(new DailyCount { Day = count.Day.Value, Games = count.Games.Value, Groups = count.Groups.Value, Users = count.players.Value });
                    db.SaveChanges();
                    Console.WriteLine("Done");

                }
            }
            catch(Exception e)
            {
                while (e.InnerException != null)
                    e = e.InnerException;

                Console.WriteLine(e.Message);
            }
        }

        static void GroupStats(long groupid)
        {
            Console.WriteLine("Group stats for " + groupid);
            try
            {
                var runStats = false;
                using (var db = new WWContext())
                {
                    //first, check that we should even run stats on this group
                    var grp = db.Groups.FirstOrDefault(x => x.GroupId == groupid);
                    var stat = db.GroupStats.FirstOrDefault(x => x.GroupId == groupid);

                    if (grp == null) //that's a problem..
                        return;
                    if (stat == null)
                        runStats = true;
                    else
                    {
                        if ((grp.Games.OrderByDescending(x => x.TimeEnded).FirstOrDefault()?.TimeEnded ??
                             DateTime.MinValue) > (stat.LastRun ?? DateTime.MinValue.AddSeconds(1)))
                        {
                            runStats = true;
                        }
                    }

                    if (!runStats) return;
                    var gamesPlayed = grp.Games.Count;
                    var night1death = db.GroupNight1Death(groupid).FirstOrDefault();
                    var day1lynch = db.GroupDay1Lynch(groupid).FirstOrDefault();
                    var day1death = db.GroupDay1Death(groupid).FirstOrDefault();
                    var survivor = db.GroupSurvivor(groupid).FirstOrDefault();

                    if (stat == null)
                    {
                        stat = db.GroupStats.Create();
                        stat.GroupId = groupid;
                        db.GroupStats.Add(stat);
                    }

                    stat.GroupName = grp.Name;
                    //TODO add this metric later
                    //stat.PlayersKilled = db.GamePlayers.Count(x => !x.Survived);
                    //stat.PlayersSurvived = db.GamePlayers.Count(x => x.Survived);

                    if (survivor != null)
                    {
                        stat.BestSurvivor = survivor.Name;
                        stat.BestSurvivorPercent = (int) survivor.pct;
                    }
                    stat.GamesPlayed = gamesPlayed;
                    stat.LastRun = DateTime.Now;
                    if (day1death != null)
                    {
                        stat.MostDeadFirstDay = day1death.Name;
                        stat.MostDeadFirstPercent = day1death.pct;
                    }
                    if (night1death != null)
                    {
                        stat.MostKilledFirstNight = night1death.Name;
                        stat.MostKilledFirstPercent = night1death.pct;
                    }
                    if (day1lynch != null)
                    {
                        stat.MostLynchedFirstNight = day1lynch.Name;
                        stat.MostLynchFirstPercent = day1lynch.pct;
                    }
                    db.SaveChanges();

                }
            }
            catch
            {
                // ignored
            }
        }

        static void PlayerStats(int playerid)
        {
            try
            {

            }
            catch
            {
                // ignored
            }
        }
    }
}
