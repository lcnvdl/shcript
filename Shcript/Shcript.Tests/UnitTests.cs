using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shcript.Lib;
using System.Text;

namespace Shcript.Tests
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void PrintTestShouldPrintTest()
        {
            var cr = new CodeRunner();
            var test = "";
            cr.Run("print(\"Test\");", t =>
            {
                test = t;
            }, e =>
            {
                throw new Exception(e);
            });

            Assert.AreEqual("Test", test);
        }

        [TestMethod]
        public void SystemDiagnosticTest()
        {
            var cr = new CodeRunner();
            cr.Run("var process = System.Diagnostics.Process.Start(\"cmd\"); process.Kill();", t =>
            {
            }, e =>
            {
                throw new Exception(e);
            });
        }

        [TestMethod]
        public void IncludeEmbedded_RunScript_Ok()
        {
            var cr = new CodeRunner();
            var output = "";
            cr.Run("class Script { include Run; public void Main(Action<string> print, object b) { print(runbatch(\"dir\")); } }", t =>
            {
                output += t;
            }, e =>
            {
                throw new Exception(e);
            });

            Assert.IsTrue(output.Length > 0);
        }

        [TestMethod]
        public void IncludeEmbedded_ThreadScript_Ok()
        {
            var cr = new CodeRunner();
            var output = "";
            cr.Run("class Script { include Thread; public void Main(Action<string> print, object b) { var t = addRunThread(()=>print(\"Hilo!\")); waitThread(t); } }", t =>
            {
                output += t;
            }, e =>
            {
                throw new Exception(e);
            });

            Assert.IsTrue(output.Length > 0);
            Assert.AreEqual("Hilo!", output);
        }

        [TestMethod]
        public void ImportEmbedded_RunScript_Ok()
        {
            var cr = new CodeRunner();
            var output = "";
            cr.Run("import Run; class Script { public void Main(Action<string> print, object b) { print(runbatch(\"dir\")); } }", t =>
            {
                output += t;
            }, e =>
            {
                throw new Exception(e);
            });

            Assert.IsTrue(output.Length > 0);
        }

        [TestMethod]
        public void ImportEmbedded_ThreadScript_Ok()
        {
            var cr = new CodeRunner();
            var output = "";
            cr.Run("import Thread; class Script { public void Main(Action<string> print, object b) { var t = addRunThread(()=>print(\"Hilo!\")); waitThread(t); } }", t =>
            {
                output += t;
            }, e =>
            {
                throw new Exception(e);
            });

            Assert.IsTrue(output.Length > 0);
            Assert.AreEqual("Hilo!", output);
        }

        [TestMethod]
        public void ImportEmbedded_ThreadScript_B_Ok()
        {
            var cr = new CodeRunner();
            var output = "";
            var thread = "addRunThread(()=>{{ int i = 0; while(i++<10){{print(\"{0}\");System.Threading.Thread.Sleep(10);}} }}); ";
            var sb = new StringBuilder();
            sb.Append("import Thread; class Script { public void Main(Action<string> print, object b) { ");
            sb.AppendFormat(thread, 1);
            sb.AppendFormat(thread, 2);
            sb.AppendFormat(thread, 3);
            sb.Append(" waitThreads(); } }");

            cr.Run(sb.ToString(), t =>
            {
                output += t;
            }, e =>
            {
                throw new Exception(e);
            });

            Assert.IsTrue(output.Length > 0);
            Assert.IsTrue(output.Contains("1"));
            Assert.IsTrue(output.Contains("2"));
            Assert.IsTrue(output.Contains("3"));
        }

        [TestMethod]
        public void ScriptWithArguments()
        {
            var cr = new CodeRunner();
            var output = "";
            var args = new System.Collections.Generic.List<string>()
            {
                "Test",
                "Test2"
            };
            cr.Run("foreach(var arg in arguments) print(arg);", t =>
            {
                output += t;
            }, e =>
            {
                throw new Exception(e);
            }, args);

            Assert.IsTrue(output.Length > 0);
            Assert.AreEqual("TestTest2", output);
        }

        [TestMethod]
        public void ScriptWithUsings()
        {
            Assert.Inconclusive();
        }
    }
}
