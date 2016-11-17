using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Database;
using Telegram.Bot.Types;

namespace Werewolf_Control.Helpers
{
    public static class Updater
    {
        private static BuildConfiguration[] Builds = new[]
        {
            new BuildConfiguration
            {
                BotDirSuffix = "Beta",
                BuildName = "Beta",
                ControlExeName = "Werewolf Control Beta"
            },
            new BuildConfiguration
            {
                BotDirSuffix = "Bot 1",
                BuildName = "Release",
                ControlExeName = "Werewolf Control"
            },
            new BuildConfiguration
            {
                BotDirSuffix = "Bot 2",
                BuildName = "Release 2",
                ControlExeName = "Werewolf Control 2"
            },
        };
        
        public static void DoUpdate(CallbackQuery query)
        {
            var msg = "Beginning file moving...";
            var updateType = query.Data.Split('|')[1];
            try
            {

                Bot.ReplyToCallback(query, msg);
                //directories
                var uDir = "c:\\build\\";
                var controlDir = uDir + "Werewolf Control\\bin\\";
                var nodeDir = uDir + "Werewolf Node\\bin\\";

                var botBaseDir = "c:\\BOT\\Werewolf 4.0 ";


                //files
                var baseFiles = new[]
                {
                    "Database.dll", "Database.pdb", "TcpFramework.dll", "TcpFramework.pdb", "Telegram.Bot.dll",
                    "Telegram.Bot.xml"
                };
                //control has different names for each bot
                //node we will just copy the entire folder

                //stage the control files in the update folder
                foreach (var b in Builds)
                {
                    if (updateType.StartsWith("beta") && b.BuildName != "Beta")
                        continue; //if beta update, don't update release

                    if (!updateType.Contains("nodes")) //if nodes only, don't update control
                    {
                        foreach (
                            var file in
                                Directory.GetFiles(controlDir + b.BuildName)
                                    .Where(
                                        x =>
                                            baseFiles.Contains(Path.GetFileName(x)) ||
                                            Path.GetFileName(x).Contains(b.ControlExeName))
                            )
                        {
                            var fName = Path.GetFileName(file);
                            System.IO.File.Copy(file, botBaseDir + b.BotDirSuffix + "\\Control\\Update\\" + fName, true);
                        }
                        msg += $"\nCopied {b.BuildName} Control files";
                        Bot.ReplyToCallback(query, msg);
                    }

                    if (!updateType.Contains("control")) //if control only, don't update nodes
                    {
                        //now find the oldest node folder

                        var copied = false;

                        foreach (
                            var d in Directory.GetDirectories(botBaseDir + b.BotDirSuffix, "*Node*"))
                        {
                            //get the version of werewolf
                            //copy the node files to it
                            foreach (var file in Directory.GetFiles(nodeDir + b.BuildName))
                            {
                                var fName = Path.GetFileName(file);
                                copied = true;
                                try
                                {
                                    System.IO.File.Copy(file, Path.Combine(d, fName), true);
                                }
                                catch (Exception e)
                                {
                                    if (e.Message.Contains("because it is being used by another process"))
                                        //nodes in this folder are still active D:
                                    {
                                        copied = false;
                                        break;
                                    }
                                    else
                                    {
                                        throw;
                                    }
                                }

                            }

                            if (copied)
                            {
                                msg += $"\nCopied {b.BuildName} Node files to " + d.Substring(d.LastIndexOf("\\") + 1);
                                Bot.ReplyToCallback(query, msg);
                                break;
                            }
                        }

                        if (!copied)
                            throw new Exception("Unable to copy Node files to a directory.");
                    }


                }


                //tell each bot to replace nodes

                //tell each bot to update
                msg += "\n\nCompleted Call, bots should now auto load updated files";
                Bot.ReplyToCallback(query, msg);
            }
            catch (Exception e)
            {
                Bot.ReplyToCallback(query, msg + "\n" + e.Message);
            }
        }

        internal static async void MonitorUpdates()
        {
            try
            {
                var baseDirectory = Path.Combine(Bot.RootDirectory, ".."); //go up one directory
                var updateDirectory = Path.Combine(Bot.RootDirectory, "\\Update");
                var currentVersion = Bot.Nodes.Max(x => Version.Parse(x.Version));
                var currentChoice = new NodeChoice();
                while (true)
                {
                    //check nodes first
                    foreach (var dir in Directory.GetDirectories(baseDirectory, "*Node*"))
                    {
                        //get the node exe in this directory
                        var file = Directory.GetFiles(dir, "Werewolf Node.exe").First();
                        Version fvi = Version.Parse(FileVersionInfo.GetVersionInfo(file).FileVersion);
                        if (fvi > currentChoice.Version)
                        {
                            currentChoice.Path = file;
                            currentChoice.Version = fvi;
                        }
                    }
                    if (currentChoice.Version > currentVersion)
                    {
                        currentVersion = currentChoice.Version;
                        //alert dev group
                        await
                            Bot.Send($"New node with version {currentVersion} found.  Stopping old nodes.",
                                -1001077134233);
                        //kill existing nodes
                        foreach (var node in Bot.Nodes)
                            node.ShutDown();
                        await Task.Delay(500);
                        foreach (var node in Bot.Nodes)
                            node.ShutDown();
                    }

                    //now check for Control update
                    if (Directory.GetFiles(updateDirectory).Count() > 1)
                    {

                        //update available
                        //sleep 10 seconds to allow any nodes to connect and whatnot.
                        await Task.Delay(10000);
                        await Bot.Send($"New control found.  Updating.", -1001077134233);
                        //fire off the updater
                        Process.Start(Path.Combine(Bot.RootDirectory, "Resources\\update.exe"), "-1001077134233");
                        Bot.Running = false;
                        Program.Running = false;
                        Bot.Api.StopReceiving();
                        //Thread.Sleep(500);
                        using (var db = new WWContext())
                        {
                            var bot =
#if DEBUG
                                4;
#elif BETA
                        3;
#elif RELEASE
                        1;
#elif RELEASE2
                        2;
#endif
                            var status = await db.BotStatus.FindAsync(bot);
                            status.BotStatus = "Updating";
                            await db.SaveChangesAsync();
                        }
                        Environment.Exit(1);
                    }


                    //check once every 5 seconds
                    await Task.Delay(5000);
                }
                //now we have the most recent version, launch one

            }
            catch (Exception e)
            {
                Bot.Send($"Error in update monitor: {e.Message}\n{e.StackTrace}", -1001077134233);
            }
        }
     
    }

    class BuildConfiguration
    {
        public string BuildName { get; set; }
        public string BotDirSuffix { get; set; }
        public string ControlExeName { get; set; }
    }
}
