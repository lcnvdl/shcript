using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Shcript.Lib.Internal
{
    public class ImportInfo
    {
        private const string startRgx = @"^\s*//\s*<\s*([A-Za-z]+)\s*>";
        private const string endRgx = @"^\s*//\s*</\s*([A-Za-z]+)\s*>";

        public List<string> Functions { get; set; }
        public List<string> Usings { get; set; }
        public List<string> Classes { get; set; }

        public ImportInfo()
        {
            Functions = new List<string>();
            Usings = new List<string>();
            Classes = new List<string>();
        }

        public void Analize(string code)
        {
            Functions.Clear();
            Usings.Clear();
            Classes.Clear();

            var section = "";
            var lines = code.Split('\n');
            var rgxStart = new Regex(startRgx);
            var rgxEnd = new Regex(endRgx);

            for (int i = 0; i < lines.Length; i++)
            {
                var l = lines[i];
                if (string.IsNullOrEmpty(l))
                    continue;

                if (string.IsNullOrEmpty(section))
                {
                    var match = rgxStart.Match(l);
                    if (match.Success)
                    {
                        section = match.Groups[1].Value;
                    }
                }
                else
                {
                    var match = rgxEnd.Match(l);
                    if (match.Success)
                    {
                        section = string.Empty;
                    }
                    else
                    {
                        if (l.Trim().StartsWith("//"))
                            continue;

                        switch (section.ToLower())
                        {
                            case "funcion":
                            case "function":
                            case "functions":
                            case "funciones":
                                Functions.Add(l);
                                break;
                            case "class":
                            case "classes":
                            case "clase":
                            case "clases":
                                Classes.Add(l);
                                break;
                            case "using":
                            case "usings":
                                Usings.Add(l);
                                break;
                        }
                    }
                }
            }
        }
    }
}
