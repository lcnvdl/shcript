using Microsoft.CSharp;
using Shcript.Lib.Code;
using Shcript.Lib.Constants;
using Shcript.Lib.Helpers;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Shcript.Lib
{
    public class CodeRunner
    {
        #region Constants

        private static readonly string[] basicUsings = new string[] {
            "System",
            "System.Collections.Generic",
            "System.Text",
            "System.Text.RegularExpressions",
            "System.Linq",
            "System.IO"
        };

        #endregion

        #region Properties

        public bool Verbose { get; set; }

        #endregion

        #region Ctor

        public CodeRunner()
        {
            Verbose = false;
        }

        #endregion

        #region Public methods

        public void RunFile(string file, Action<string> printText, Action<string> printError, List<string> arguments)
        {
            Run(File.ReadAllText(file), printText, printError, arguments);
        }

        public void Run(string script, Action<string> printText, Action<string> printError, List<string> arguments = null)
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
                printText("---------------------");
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
            if (mi.GetParameters().Length == 2)
            {
                mi.Invoke(o, new object[] { printText, printError });
            }
            else
            {
                mi.Invoke(o, new object[] { printText, printError, (arguments ?? new List<string>()).ToArray() });
            }
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
                if (info.Classes.Count > 0)
                {
                    var classesCode = new StringBuilder();

                    foreach (var @class in info.Classes)
                    {
                        classesCode.AppendLine(@class);
                    }

                    code = classesCode.ToString() + code;
                }
            }

            //  Add usings
            foreach (var @using in usings)
            {
                outSb.AppendLine(@using);
            }
            
            //  Add the script class if does not exists
            if (isScript && !Regex.IsMatch(code, Rgx.Code.ScriptClass))
            {
                outSb.AppendLine("public class Script {");
                outSb.AppendLine("public void Main(System.Action<string> print, System.Action<string> printError, string[] arguments) {");
                outSb.AppendLine(code);
                outSb.AppendLine("} }");
            }
            else
            {
                //  TODO    Agregar Script padre, que no tengan que agregar el método Main con esos parámetros.
                outSb.AppendLine(code);
            }

            code = outSb.ToString();

            //  Add extra functions
            {
                Match begin = Regex.Match(code, Rgx.Code.Begin);
                if (begin.Success)
                {
                    var main = Regex.Matches(code, Rgx.Code.Main).Cast<Match>().OrderBy(m => m.Index).First(m => m.Index > begin.Index);
                    var sb = new StringBuilder();
                    sb.AppendLine();
                    foreach (var info in infos)
                    {
                        foreach(var fn in info.Functions)
                        {
                            sb.AppendLine(fn);
                        }

                        sb.AppendLine();
                    }

                    code = code.Insert(main.Index - 1, sb.ToString());
                }
            }

            return code;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Extracts usings from a script, splitting it into a list (return) and the rest of code (out code).
        /// </summary>
        /// <param name="script"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        private List<string> ParseUsings(string script, out string code)
        {
            var usings = new List<string>();
            var lastMatch = 0;

            foreach (Match match in Regex.Matches(script, Rgx.Code.Using))
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

                string content = FilesHelper.GetContent(path);

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

                string content = FilesHelper.GetContent(path);

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

        #endregion
    }
}
