using System;
using System.Collections.Generic;
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
                ControlExeName = "Werewolf Control Beta.exe"
            },
            new BuildConfiguration
            {
                BotDirSuffix = "Bot 1",
                BuildName = "Release",
                ControlExeName = "Werewolf Control.exe"
            },
            new BuildConfiguration
            {
                BotDirSuffix = "Bot 2",
                BuildName = "Release 2",
                ControlExeName = "Werewolf Control 2.exe"
            },
        };
        
        public static void DoUpdate(CallbackQuery query)
        {
            var msg = "Beginning file moving...";
            Bot.ReplyToCallback(query,msg);
            //directories
            var uDir = "c:\\build\\";
            var controlDir = uDir + "Werewolf Control\\bin\\";
            var nodeDir = uDir + "Werewolf Node\\bin\\";

            var botBaseDir = "c:\\BOT\\Werewolf 4.0 ";
            

            //files
            var baseFiles = new[] {"Database.dll", "Database.pdb","TcpFramework.dll", "TcpFramework.pdb", "Telegram.Bot.dll", "Telegram.Bot.xml"};
            //control has different names for each bot
            //node we will just copy the entire folder

            //stage the control files in the update folder
            foreach (var b in Builds)
            {
                foreach (
                    var file in
                        Directory.GetFiles(uDir + controlDir + b.BuildName)
                            .Where(
                                x => baseFiles.Contains(Path.GetFileName(x)) || Path.GetFileName(x) == b.ControlExeName)
                    )
                {
                    var fName = Path.GetFileName(file);
                    System.IO.File.Copy(file, botBaseDir + b.BotDirSuffix + "\\Control\\Update\\" + fName);
                }
                msg += "\nCopied Control files for " + b.BotDirSuffix;
                Bot.ReplyToCallback(query, msg);
                //now find the oldest node folder

                //copy the node files to it
            }





            //tell each bot to replace nodes

            //tell each bot to update
            msg += "\n\nCompleted Call";
            Bot.ReplyToCallback(query, msg);
        }
    }

    class BuildConfiguration
    {
        public string BuildName { get; set; }
        public string BotDirSuffix { get; set; }
        public string ControlExeName { get; set; }
    }
}
