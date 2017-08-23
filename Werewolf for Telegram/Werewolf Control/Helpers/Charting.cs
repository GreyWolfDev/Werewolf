using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using Database;
using Telegram.Bot.Types;
using Werewolf_Control.Handler;
using Werewolf_Control.Models;

namespace Werewolf_Control.Helpers
{
    public static class Charting
    {
        public static void TeamWinChart(string input, Update u)
        {
            //first we need to get the start date / timespan, otherwise default.
            var start = new DateTime(2016, 5, 15);
            var mode = "";
            if (!String.IsNullOrWhiteSpace(input))
            {
                var args = input.Split(' ');
                int amount = 0;
                if (int.TryParse(args[0], out amount) && args.Length >= 2)
                {
                    //get the interval
                    switch (args[1])
                    {
                        case "weeks":
                        case "week":
                            start = DateTime.UtcNow.AddDays(-(amount * 7));
                            break;
                        case "day":
                        case "days":
                            start = DateTime.UtcNow.AddDays(-amount);
                            break;
                        case "hour":
                        case "hours":
                            start = DateTime.UtcNow.AddHours(-amount);
                            break;
                        default:
                            Bot.Api.SendTextMessageAsync(u.Message.Chat.Id, "Acceptable intervals are: hour(s), day(s), week(s)");
                            break;
                    }
                }
                
                if (args.Length == 3)
                    mode = $"AND gm.MODE = '{args[2]}'";

                if (args.Length == 1)
                    mode = $"AND gm.MODE = '{args[0]}'";
            }
            
            var query = $@"SELECT x.Players,
 Round((COUNT (x.GameId) * 100.0 / sum (count(x.GameId)) OVER (PARTITION BY Players)), 2) AS Wins,
sum(count(x.Gameid)) over (partition by players) as Games
 , X.Winner AS Team
 FROM
 (SELECT count (gp.PlayerId) AS Players, gp.GameId, CASE WHEN gm.Winner = 'Wolves' THEN 'Wolf' ELSE gm.Winner END AS Winner
 FROM Game AS gm
 INNER JOIN GamePlayer AS gp ON gp.GameId = gm.Id
 WHERE gm.Winner is not null AND gm.TimeStarted > '{ start.ToString("yyyy-MM-dd HH:mm:ss")}' {mode}
 GROUP BY gp.GameId, gm.Winner
 HAVING COUNT (gp.PlayerId)> = 5)
 AS x 
 GROUP BY x.Winner, x.Players
 ORDER BY x.Players, Wins DESC";

            var result = new List<TeamWinResult>();
            using (var db = new WWContext())
            {
                result = db.Database.SqlQuery<TeamWinResult>(query).ToListAsync().Result;
            }

            //we have our results, now chart it...
            //build a datatable
            var dataSet = new DataSet();
            var dt = new DataTable();
            dt.Columns.Add("Players", typeof(int));
            dt.Columns.Add("Wins", typeof(int));
            dt.Columns.Add("Games", typeof(int));
            dt.Columns.Add("Team", typeof(string));
            
            foreach (var r in result)
            {
                var row = dt.NewRow();
                row[0] = r.Players;
                row[1] = (int)r.Wins;
                row[2] = r.Games;
                row[3] = r.Team;
                dt.Rows.Add(row);
            }

            dataSet.Tables.Add(dt);

            //now build the chart
            Chart chart = new Chart();
            //chart.DataSource = dataSet.Tables[0];
            chart.Width = 1000;
            chart.Height = 400;
            var legend = new Legend();
            //create serie...
            foreach (var team in new[] { "Wolf", "Village", "Tanner", "Cult", "SerialKiller", "Lovers" })
            {
                Series serie1 = new Series();
                //serie1.Label = team;
                serie1.LegendText = team;
                serie1.Name = team;
                switch (team)
                {
                    case "Wolf":
                        serie1.Color = Color.SaddleBrown;
                        break;
                    case "Village":
                        serie1.Color = Color.Green;
                        break;
                    case "Tanner":
                        serie1.Color = Color.Red;
                        break;
                    case "Cult":
                        serie1.Color = Color.Blue;
                        break;
                    case "SerialKiller":
                        serie1.Color = Color.Black;
                        break;
                    case "Lovers":
                        serie1.Color = Color.Pink;
                        break;
                }
                serie1.MarkerBorderWidth = 2;
                serie1.BorderColor = Color.FromArgb(164, 164, 164);
                serie1.ChartType = SeriesChartType.StackedBar100;
                serie1.BorderDashStyle = ChartDashStyle.Solid;
                serie1.BorderWidth = 1;
                //serie1.ShadowColor = Color.FromArgb(128, 128, 128);
                //serie1.ShadowOffset = 1;
                serie1.IsValueShownAsLabel = false;
                serie1.XValueMember = "Players";
                serie1.YValueMembers = "Wins";
                serie1.Font = new Font("Tahoma", 8.0f);
                serie1.BackSecondaryColor = Color.FromArgb(0, 102, 153);
                serie1.LabelForeColor = Color.FromArgb(100, 100, 100);
                //add our values
                var pl = 4;
                foreach (var r in result.Where(x => x.Team == team).OrderBy(x => x.Players))
                {
                    pl++;
                    if (r.Players != pl)
                    {
                        while (pl < r.Players)
                        {
                            serie1.Points.AddXY(pl, 0);
                            pl++;
                        }
                    }
                    serie1.Points.AddXY(r.Players, r.Wins);
                }
                //make sure we filled all the points...
                var top = (int)(serie1.Points.OrderByDescending(x => x.XValue).FirstOrDefault()?.XValue ?? 4);

                if (top < 35)
                {
                    top++;
                    while (top <= 35)
                    {
                        serie1.Points.AddXY(top, 0);
                        top++;
                    }
                }
                //legend.CustomItems.Add(serie1.Color, team);
                chart.Series.Add(serie1);
            }
            //create chartareas...
            ChartArea ca = new ChartArea();
            ca.Name = "ChartArea1";
            ca.BackColor = Color.White;
            ca.BorderColor = Color.FromArgb(26, 59, 105);
            ca.BorderWidth = 0;
            ca.BorderDashStyle = ChartDashStyle.Solid;
            ca.AxisX = new Axis();
            ca.AxisY = new Axis();
            chart.ChartAreas.Add(ca);
            chart.Legends.Add(legend);
            //databind...
            //chart.DataBind();
            //save result




            var path = Path.Combine(Bot.RootDirectory, "myChart.png");
            chart.SaveImage(path, ChartImageFormat.Png);
            SendImage(path, u.Message.Chat.Id);
            UpdateHandler.Send(result.Select(x => new {Players = x.Players, Games = x.Games}).Distinct().Aggregate("", (a, b) => $"{a}\n{b.Players}: {b.Games}"), u.Message.Chat.Id);
        }

        private static void SendImage(string path, long id)
        {
            var fs = new FileStream(path, FileMode.Open);
            Bot.Api.SendPhotoAsync(id, new FileToSend("chart.png", fs));
        }
    }

    class TeamWinResult
    {

        public int Players { get; set; }
        public Decimal Wins { get; set; }
        public int Games { get; set; }
        public string Team { get; set; }
        
    }
}
