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

            var console = string.IsNullOrEmpty(file);
            var cr = new CodeRunner();
            cr.Verbose = verbose;

            bool error = console ? RunConsole(cr, scriptArguments) : RunFile(cr, scriptArguments, file);

            Environment.Exit(error ? 0 : 1);
        }

        private static bool RunConsole(CodeRunner cr, List<string> scriptArguments)
        {
            bool error = false;
            bool exit = false;

            List<string> accumulator = new List<string>();

            while (!exit)
            {
                Console.Write("Shcript> ");

                string line = Console.ReadLine();

                if (string.IsNullOrEmpty(line) || ParseInternalCommands(line, ref exit))
                    continue;

                while (line.Trim().EndsWith("\\"))
                {
                    accumulator.Add(line.Remove(line.LastIndexOf('\\')));
                    line = Console.ReadLine();
                }

                if (accumulator.Count > 0)
                {
                    accumulator.Add(line);
                    line = string.Join(Environment.NewLine, accumulator);
                    accumulator.Clear();
                }
                else
                {
                    if (!line.Trim().EndsWith(";"))
                        line += ";";
                }

                try
                {
                    cr.Run(line, log => Console.WriteLine(log), err => { Console.WriteLine(err); error = true; }, scriptArguments);
                }
                catch (Exception ex)
                {
                    error = true;
                    Console.WriteLine(ex.ToString());
                }
            }

            Console.WriteLine("Closed" + (error ? " with error" : ""));

            return error;
        }

        private static bool ParseInternalCommands(string line, ref bool exit)
        {
            bool result = true;

            switch (line.ToLower().Trim())
            {
                case "exit":
                    exit = true;
                    break;

                case "cls":
                case "clear":
                case "clean":
                    Console.Clear();
                    break;

                default:
                    result = false;
                    break;
            }

            return result;
        }

        private static bool RunFile(CodeRunner cr, List<string> scriptArguments, string file)
        {
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

            return error;
        }
    }
}
