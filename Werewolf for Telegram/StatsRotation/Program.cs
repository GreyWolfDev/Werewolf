using System;
using System.Collections.Generic;
using System.Linq;
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
                    Thread.Sleep(5000);


                    //now groups
                    List<int> groupids;
                    using (var db = new WWContext())
                    {
                        groupids = db.Groups.Select(x => x.Id).ToList();
                    }
                    foreach (var g in groupids)
                    {
                        GroupStats(g);
                        Thread.Sleep(5000);
                    }

                    //players
                    List<int> playerids;
                    using (var db = new WWContext())
                    {
                        playerids = db.Players.Select(x => x.Id).ToList();
                    }
                    foreach (var p in playerids)
                    {
                        PlayerStats(p);
                        Thread.Sleep(5000);
                    }
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
                using (var DB = new WWContext())
                {

                    var gamesPlayed = DB.Games.Count();
                    var night1death = DB.GlobalNight1Death().First();
                    var day1lynch = DB.GlobalDay1Lynch().First();
                    var day1death = DB.GlobalDay1Death().First();
                    var survivor = DB.GlobalSurvivor().First();
                    var stat = DB.GlobalStats.FirstOrDefault();
                    if (stat == null)
                    {
                        stat = DB.GlobalStats.Create();
                        DB.GlobalStats.Add(stat);
                    }

                    stat.PlayersKilled = DB.GamePlayers.Count(x => !x.Survived);
                    stat.PlayersSurvived = DB.GamePlayers.Count(x => x.Survived);

                    stat.BestSurvivor = DB.Players.Find(survivor.playerid).Name;
                    stat.BestSurvivorPercent = (int)survivor.pct;
                    stat.GamesPlayed = gamesPlayed;
                    stat.LastRun = DateTime.Now;
                    stat.MostKilledFirstDay = DB.Players.Find(day1death.victimid).Name;
                    stat.MostKilledFirstDayPercent = day1death.pct;
                    stat.MostKilledFirstNight = DB.Players.Find(night1death.victimid).Name;
                    stat.MostKilledFirstPercent = night1death.pct;
                    stat.MostLynchedFirstDay = DB.Players.Find(day1lynch.victimid).Name;
                    stat.MostLynchedFirstPercent = day1lynch.pct;
                    DB.SaveChanges();
                    
                }
            }
            catch(Exception e)
            {
                e = e;
            }
        }

        static void GroupStats(int groupid)
        {
            try
            {

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
