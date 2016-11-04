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
                    Version oldVersion = new Version(99,99);
                    var oldest = "";
                    foreach (
                        var d in Directory.GetDirectories(botBaseDir + b.BotDirSuffix, "*Node*"))
                    {
                        //get the version of werewolf
                        var file = Directory.GetFiles(d, "Werewolf Node.exe").First();
                        Version fvi = Version.Parse(FileVersionInfo.GetVersionInfo(file).FileVersion);
                        if (fvi < oldVersion)
                        {
                            oldest = d;
                            oldVersion = fvi;
                        }
                    }
                    if (String.IsNullOrEmpty(oldest))
                        throw new Exception("Could not determine oldest Node directory :(");
                    
                    //copy the node files to it
                    foreach (var file in Directory.GetFiles(nodeDir + b.BuildName))
                    {
                        var fName = Path.GetFileName(file);
                        System.IO.File.Copy(file, Path.Combine(oldest, fName), true);
                    }
                    msg += "\nCopied Node files to " + oldest.Substring(oldest.LastIndexOf("\\") + 1);
                    Bot.ReplyToCallback(query, msg);
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
