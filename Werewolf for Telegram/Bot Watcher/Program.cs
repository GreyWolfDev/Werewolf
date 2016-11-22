using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bot_Watcher
{
    class Program
    {
        internal static string RootDirectory => Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));

        static void Main()
        {
            while (true)
            {
                Thread.Sleep(500);
                //check to see if the bot updater is running.  If so, ignore.
                try
                {
                    if (Process.GetProcessesByName("update").Any())
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("UPDATING");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        continue;
                    }
                    Console.Clear();
                    //using a text file so it can be changed as needed
                    using (var sr = new StreamReader(RootDirectory + "\\processes.txt"))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            var processName = line.Substring(line.LastIndexOf("\\", StringComparison.Ordinal) + 1);
                            var processPath = line.Replace(processName, "");
                            processName = processName.Replace(".exe", "");
                            Console.WriteLine(processName);
                            //var updating = false;
                            //foreach (var p in Process.GetProcessesByName("Update"))
                            //{
                            //    var path = p.MainModule.FileName.Replace("Resources\\update.exe", "");
                            //    updating = String.Equals(path, processPath, StringComparison.Ordinal);
                            //}
                            //if (updating)
                            //{
                            //    Console.ForegroundColor = ConsoleColor.Green;
                            //    Console.WriteLine("UPDATING");
                            //    Console.ForegroundColor = ConsoleColor.Gray;
                            //}
                            
                            if (Process.GetProcessesByName(processName).Any()) continue;
                            

                            //if (!updating)
                                Process.Start(line);
                            
                                
                            
                        }
                    }
                }
                catch
                {
                    //ignored
                }

                
            }
        }
    }
}
