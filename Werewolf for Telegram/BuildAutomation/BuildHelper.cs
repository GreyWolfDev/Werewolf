using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BuildAutomation
{
    public static class BuildHelper
    {
        public static async void Automate()
        {
            //TODO: Pull latest files from repo
            var p = new Process
            {
                StartInfo =
                    {
                        FileName = @"C:\Werewolf Source\Werewolf\sync repo.bat",
                        WorkingDirectory = @"C:\Werewolf Source\Werewolf",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
            };
            p.Start();
            p.WaitForExit();
            //TODO: Build each version (Beta, Release, Release 2)
            //methinks I'm gonna need to install VS for this
            //TODO: Stage update for control

            //TODO: Stage Node update

            //TODO: Send a message to dev chat, notifying updates are staged
        }
    }
}