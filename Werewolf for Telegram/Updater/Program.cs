using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Updater
{
    class Program
    {
        internal static string RootDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
        static void Main(string[] args)
        {
            //ignore this commit
            try
            {
                var id = "";
                if (args.Length > 0)
                    id = args[0];
                Console.WriteLine($"Update called from {id}");
                //get the root directory for the control
                var mainPath = Path.Combine(RootDirectory, @"..\");

                Console.WriteLine("Waiting on bot to exit....");
                //first, wait for the bot to close out
                var botName = 
#if RELEASE2
                    "Werewolf Control 2";
#elif RELEASE
                    "Werewolf Control";
#else
                    "Werewolf Control Beta";
#endif
                while (Process.GetProcessesByName(botName).Any())
                {
                    Thread.Sleep(100);
                }
                Console.WriteLine("Patching...");
                Thread.Sleep(500);
                //ok, it's off, patch it
                foreach (var file in Directory.GetFiles(Path.Combine(mainPath, "update")))
                {
                    Console.WriteLine(file);
                    File.Copy(file, file.Replace("\\update", ""), true);
                    File.Delete(file);
                }
                Console.WriteLine("Starting bot....");
                //now start it back up
                //if (!Process.GetProcessesByName("Werewolf Control").Any())
                var path = Path.Combine(mainPath,
#if RELEASE2
                    "werewolf control 2.exe"
#elif RELEASE
                    "werewolf control.exe"
#else
                    "Werewolf Control Beta.exe"
#endif
                    );
                Process.Start(path, id);
                Console.WriteLine("Update complete");
                Thread.Sleep(5000);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Thread.Sleep(-1);
            }
        }
    }
}
