using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace BuildAutomation
{
    public static class BuildHelper
    {
        public static Task Automate()
        {
            try
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
                string output = "", error = "";
                while (!p.StandardOutput.EndOfStream)
                    output += p.StandardOutput.ReadLine() + Environment.NewLine;
                while (!p.StandardError.EndOfStream)
                    output += p.StandardError.ReadLine() + Environment.NewLine;

                

                using (var sw = new StreamWriter(HttpContext.Current.Server.MapPath("~/App_Data/build.log")))
                {
                    sw.WriteLine($"Git Pull\n{output}");
                }
                if (output.Contains("error"))
                {
                    throw new HttpException("Unable to pull repo\n" + output);
                }
                p.WaitForExit();
                //TODO: Build each version (Beta, Release, Release 2) test
                //methinks I'm gonna need to install VS for this
                //TODO: Stage update for control

                //TODO: Stage Node update

                //TODO: Send a message to dev chat, notifying updates are staged


                return Task.FromResult(true);
            }
            catch (HttpException e)
            {
                throw e;
            }
            catch (Exception e)
            {

                using (var sw = new StreamWriter(HttpContext.Current.Server.MapPath("~/App_Data/error.log"), true))
                {
                    sw.WriteLine($"----------------------------------------------------------\n{DateTime.Now}\n{e.Message}\n{e.StackTrace}\n");
                    while (e.InnerException != null)
                    {
                        e = e.InnerException;
                        sw.WriteLine($"\n{e.Message}\n");
                    }
                }

                throw new HttpException(e.Message, e.InnerException);
            }
        }
    }
}