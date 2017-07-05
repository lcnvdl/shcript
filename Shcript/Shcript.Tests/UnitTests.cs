using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shcript.Lib;

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
        public void IncludeEmbedded()
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
        public void ImportEmbedded()
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
        public void ScriptWithUsings()
        {
            Assert.Inconclusive();
        }
    }
}
