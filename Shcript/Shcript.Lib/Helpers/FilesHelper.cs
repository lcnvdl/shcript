using System.IO;
using System.Net;
using System.Reflection;

namespace Shcript.Lib.Helpers
{
    internal static class FilesHelper
    {
        public static string GetContent(string path)
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

        private static string ReadEmbebbed(string path)
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

        private static string DownloadText(string url)
        {
            using (WebClient client = new WebClient())
            {
                return client.DownloadString(url);
            }
        }
    }
}
