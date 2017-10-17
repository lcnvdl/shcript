using Shcript.Lib;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shcript.UI
{
    class Program
    {
        static void Main(string[] args)
        {
            string file = string.Empty;
            bool verbose = false;
            List<string> scriptArguments = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                var cmd = args[i].ToLower();
                switch (cmd)
                {
                    case "-a":
                    case "--argument":
                        {
                            scriptArguments.Add(args[++i]);
                        }
                        break;

                    case "-v":
                    case "--verbose":
                        {
                            verbose = true;
                        }
                        break;
                    case "-w":
                    case "--working-dir":
                        {
                            string d = args[++i];
                            if (!Directory.Exists(d))
                            {
                                Console.WriteLine("Error, the directory " + d + " does not exist");
                                Environment.Exit(1);
                                return;
                            }

                            Environment.CurrentDirectory = d;
                        }
                        break;

                    default:
                        {
                            if (string.IsNullOrEmpty(file))
                            {
                                file = args[i];
                            }
                        }
                        break;
                }
            }

            if (string.IsNullOrEmpty(file))
                return;

            var cr = new CodeRunner();
            cr.Verbose = verbose;

            bool error = false;

            try
            {
                cr.RunFile(file, log => Console.WriteLine(log), err => { Console.WriteLine(err); error = true; }, scriptArguments);
            }
            catch (Exception ex)
            {
                error = true;
                Console.WriteLine(ex.ToString());
            }

            Environment.Exit(error ? 0 : 1);
        }
    }
}
