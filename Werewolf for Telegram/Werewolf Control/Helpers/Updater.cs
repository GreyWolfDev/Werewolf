using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                    msg += "\nCopied Control files for " + b.BotDirSuffix;
                    Bot.ReplyToCallback(query, msg);
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
                                if (e.Message.Contains("because it is being used by another process")) //nodes in this folder are still active D:
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
                            msg += "\nCopied Node files to " + d.Substring(d.LastIndexOf("\\") + 1);
                            Bot.ReplyToCallback(query, msg);
                            break;
                        }
                    }

                    if (!copied)
                        throw new Exception("Unable to copy Node files to a directory.");
                }





                //tell each bot to replace nodes

                //tell each bot to update
                msg += "\n\nCompleted Call - until this is fully automated, please run /replacenodes and /update";
                Bot.ReplyToCallback(query, msg);
            }
            catch (Exception e)
            {
                Bot.ReplyToCallback(query, msg + "\n" + e.Message);
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
