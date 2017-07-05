namespace Shcript.Lib.Constants
{
    /// <summary>
    /// Constants for regexs.
    /// </summary>
    internal static class Rgx
    {
        public class Code
        {
            public const string Using = @"\busing\s+([a-zA-Z]+\s*=\s*)?[a-zA-Z.0-9]+\s*[\;]";
            public const string Begin = @"(public\s+)?class\s+Script\s+{";
            public const string Main = @"(public\s+)?void\s+Main\s*\(\s*";
            public const string ScriptClass = @"\bclass\s+Script\s*{";
        }
    }
}
