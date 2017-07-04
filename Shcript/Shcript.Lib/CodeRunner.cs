using Microsoft.CSharp;
using Shcript.Lib.Internal;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Shcript.Lib
{
    public class CodeRunner
    {
        private const string usingRgx = @"\busing\s+([a-zA-Z]+\s*=\s*)?[a-zA-Z.0-9]+\s*[\;]";
        private const string beginRgx = @"(public\s+)?class\s+Script\s+{";
        private const string mainRgx = @"(public\s+)?void\s+Main\s*\(\s*";
        private const string scriptClassRgx = @"\bclass\s+Script\s*{";
        private static readonly string[] basicUsings = new string[] {
            "System",
            "System.Collections.Generic",
            "System.Text",
            "System.Linq",
            "System.IO"
        };

        public bool Verbose { get; set; }

        public CodeRunner()
        {
            Verbose = false;
        }

        public void RunFile(string file, Action<string> printText, Action<string> printError)
        {
            Run(File.ReadAllText(file), printText, printError);
        }

        public void Run(string script, Action<string> printText, Action<string> printError)
        {
            var provider = CSharpCodeProvider.CreateProvider("CSharp", new Dictionary<string, string> { { "CompilerVersion", "v4.0" } });

            var compilerParams = new CompilerParameters
            {
                GenerateInMemory = true,
                GenerateExecutable = false
            };

            //  TODO    Permitir referencias personalizadas.
            AddReferences(compilerParams);

            var code = ParseScript(script);
            if (Verbose)
            {
                printText("---------------------");
                printText("Parsed script:");
                printText(code);
                printText("---------------------");
            }

            var results = provider.CompileAssemblyFromSource(compilerParams, code);

            if (results.Errors.Count > 0)
            {
                foreach (CompilerError error in results.Errors)
                {
                    printError(error.ToString());
                }
            }

            var o = results.CompiledAssembly.CreateInstance("Script");
            var mi = o.GetType().GetMethod("Main");
            mi.Invoke(o, new object[] { printText, printError });
        }

        public string ParseScript(string script, bool isScript = true)
        {
            var outSb = new StringBuilder();
            string code;

            //  Parse usings and cut it from code
            var usings = ParseUsings(script, out code);

            //  Parse includes
            code = ParseIncludes(code);

            //  Parse imports
            List<ImportInfo> infos;
            code = ParseImports(code, out infos);

            foreach (var info in infos)
            {
                //  Imported Usings
                foreach (var @using in info.Usings)
                {
                    if (!usings.Any(u => u.Contains(@using)))
                    {
                        usings.Add("using " + @using + ";");
                    }
                }

                //  Imported classes
                foreach (var @class in info.Classes)
                {
                    code = "\n" + @class + "\n" + code;
                }
            }

            //  Add usings
            foreach (var @using in usings)
            {
                outSb.AppendLine(@using);
            }

            //  Add the script class if does not exists
            if (isScript && !Regex.IsMatch(code, scriptClassRgx))
            {
                outSb.AppendLine("public class Script { public void Main(System.Action<string> print, System.Action<string> printError) {");
                outSb.AppendLine(code);
                outSb.AppendLine("}}");
            }
            else
            {
                //  TODO    Agregar Script padre, que no tengan que agregar el método Main con esos parámetros.
                outSb.AppendLine(code);
            }

            code = outSb.ToString();

            //  Add extra functions
            {
                Match begin = Regex.Match(code, beginRgx);
                if (begin.Success)
                {
                    Match main = Regex.Matches(code, mainRgx).Cast<Match>().OrderBy(m => m.Index).First(m => m.Index > begin.Index);

                    foreach (var info in infos)
                    {
                        //  Imported Functions
                        code = code.Insert(main.Index - 1, string.Join("\n", info.Functions));
                    }
                }
            }

            return code;
        }

        private List<string> ParseUsings(string script, out string code)
        {
            var usings = new List<string>();
            var lastMatch = 0;

            foreach (Match match in Regex.Matches(script, usingRgx))
            {
                usings.Add(match.Value);
                if (lastMatch < match.Index + match.Length)
                {
                    lastMatch = match.Index + match.Length;
                }
            }

            foreach (var basicUsing in basicUsings)
            {
                if (!usings.Any(u => u.Contains(basicUsing)))
                {
                    usings.Add("using " + basicUsing + ";");
                }
            }

            code = script.Substring(lastMatch).Trim();

            return usings;
        }

        private string ParseImports(string code, out List<ImportInfo> infos)
        {
            infos = new List<ImportInfo>();
            while (code.Contains("import "))
            {
                int i = code.IndexOf("import ");
                int f = code.IndexOf(';', i + 1);

                string importLine = (f == code.Length - 1) ? code : code.Remove(f + 1);
                importLine = importLine.Substring(i).Trim();
                string path = importLine.Remove(importLine.Length - 1).Substring(importLine.IndexOf(' ')).Trim();

                string content = GetContent(path);

                var info = new ImportInfo();
                info.Analize(content);

                code = code.Replace(importLine, string.Empty);

                infos.Add(info);
            }

            return code;
        }

        private string ParseIncludes(string code)
        {
            while (code.Contains("include "))
            {
                int i = code.IndexOf("include ");
                int f = code.IndexOf(';', i + 1);

                string importLine = (f == code.Length - 1) ? code : code.Remove(f + 1);
                importLine = importLine.Substring(i).Trim();
                string path = importLine.Remove(importLine.Length - 1).Substring(importLine.IndexOf(' ')).Trim();

                string content = GetContent(path);

                code = code.Replace(importLine, content);
            }

            return code;
        }

        private void AddReferences(CompilerParameters compilerParams, params string[] extraReferences)
        {
            compilerParams.ReferencedAssemblies.Add("System.dll");
            compilerParams.ReferencedAssemblies.Add("System.Core.dll");
            compilerParams.ReferencedAssemblies.Add("System.Data.dll");
            compilerParams.ReferencedAssemblies.Add("System.Data.DataSetExtensions.dll");
            compilerParams.ReferencedAssemblies.Add("System.Xml.dll");
            compilerParams.ReferencedAssemblies.Add("System.Xml.Linq.dll");

            if (extraReferences.Length > 0)
            {
                compilerParams.ReferencedAssemblies.AddRange(extraReferences);
            }
        }

        private string GetContent(string path)
        {
            string content;
            if (path.StartsWith("http://"))
            {
                content = DownloadText(path);
            }
            else if (path.Contains("\\") || !path.EndsWith(".script") && path.Contains("."))
            {
                content = File.ReadAllText(path);
            }
            else
            {
                content = ReadEmbebbed(path);
            }

            return content;
        }

        private string ReadEmbebbed(string path)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Shcript.Lib.Scripts." + path.ToLower();

            if (!resourceName.EndsWith(".script"))
            {
                resourceName += ".script";
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private string DownloadText(string url)
        {
            using (WebClient client = new WebClient())
            {
                return client.DownloadString(url);
            }
        }
    }
}
