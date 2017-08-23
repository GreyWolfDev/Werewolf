using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Database;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

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
            //new BuildConfiguration
            //{
            //    BotDirSuffix = "Bot 2",
            //    BuildName = "Release 2",
            //    ControlExeName = "Werewolf Control 2"
            //},
        };


        public static void DoBuild(CallbackQuery query)
        {
            var msg = query.Message.Text + $"\n\nBeginning build...\n";
            
            Bot.ReplyToCallback(query, msg);
            //determine what we are building
            var updateType = query.Data.Split('|')[1];
            var beta = updateType.StartsWith("beta");
            var control = !updateType.Contains("node");
            var node = !updateType.Contains("control");

            msg += "Build Definition(s) to Use:";
            var definitions = new List<string>();
            var env = beta ? "Beta" : "Release";
            //var what = control ? node ? "Both" : "Control" : "Node";
            if (control)
                definitions.Add($"{env} Control");
            if (node)
                definitions.Add($"{env} Node");

            msg = definitions.Aggregate(msg, (current, a) => current + "\n" + a);
            Thread.Sleep(500);

            Bot.Edit(query, msg);
            Thread.Sleep(500);
            //now let's actually kick off that build
            msg = definitions.Aggregate(msg, (current, def) => current + ("\n" + QueueBuild(def).Result));
            Bot.Edit(query, msg);
        }


        public static void DoUpdate(CallbackQuery query)
        {
            var msg = query.Message.Text + "\n\nBeginning file moving...";
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
                    if (!updateType.StartsWith(b.BuildName.ToLower())) continue;
                    //update types can contain 'node', 'control', or 'both'
                    if (!updateType.Contains("node")) //if nodes only, don't update control
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


        public static async Task<string> QueueBuild(string buildDefinitionName)
        {
            try
            {
                var url = "https://parabola949.VisualStudio.com/DefaultCollection/";
                var build = new BuildHttpClient(new Uri(url), new VssCredentials(new VssBasicCredential("", RegHelper.GetRegValue("VSTSToken"))));

                // First we get project's GUID and buildDefinition's ID.
                // Get the list of build definitions.
                var definitions = await build.GetDefinitionsAsync(project: "Werewolf");

                // Get the specified name of build definition.
                var target = definitions.First(d => d.Name == buildDefinitionName);

                // Build class has many properties, hoqever we can set only these properties.
                //ref: https://www.visualstudio.com/integrate/api/build/builds#queueabuild
                //In this nuget librari, we should set Project property.
                //It requires project's GUID, so we're compelled to get GUID by API.
                try
                {
                    var res = await build.QueueBuildAsync(new Build
                    {
                        Definition = new DefinitionReference
                        {
                            Id = target.Id
                        },
                        Project = target.Project
                    });
                    return $"Queued build with id: {res.Id}";
                }
                catch(VssServiceException e)
                {
                    return $"{e.Message}";
                }
            }
            catch (Exception e)
            {
                var t = e.GetType();
                while (e.InnerException != null)
                    e = e.InnerException;
                return $"{e.Message}\n{e.StackTrace}";
            }
        }


        internal static async void MonitorUpdates()
        {
#if !DEBUG
            try
#endif
            {
                var baseDirectory = Path.Combine(Bot.RootDirectory, ".."); //go up one directory
                var updateDirectory = Path.Combine(Bot.RootDirectory, "Update");
                while (Bot.Nodes.Count == 0)
                    await Task.Delay(500);
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
                        Bot.Send($"New node with version {currentVersion} found.  Stopping old nodes.", -1001077134233);
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
                        //sleep 5 seconds to allow any nodes to connect and whatnot.
                        await Task.Delay(5000);
                        //await Bot.Send($"New control found.  Updating.", -1001077134233);
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

#if !DEBUG
                            var status = await db.BotStatus.FindAsync(bot);
                            status.BotStatus = "Updating";
                            await db.SaveChangesAsync();
#endif
                        }
                        Environment.Exit(1);
                    }


                    //check once every 5 seconds
                    await Task.Delay(5000);
                }
                //now we have the most recent version, launch one

            }
#if !DEBUG
            catch (Exception e)
            {
                Bot.Send($"Error in update monitor: {e.Message}\n{e.StackTrace}", -1001077134233, parseMode: ParseMode.Default);
            }
#endif
        }

    }

    class BuildConfiguration
    {
        public string BuildName { get; set; }
        public string BotDirSuffix { get; set; }
        public string ControlExeName { get; set; }
    }
}
