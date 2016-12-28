using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using TestHelper;

namespace QuickAsserts.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public void TestMethod1()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void TestMethod2()
        {
            var model = new TestClass {Inner = new InnerTestClass {Id = 1}};

            var dic = new Dictionary<string, InnerTestClass>();

            Assert.AreEqual(null, model);
            Assert.AreEqual(1, model.Inner.Id);
            Assert.AreEqual(new DateTime(2016, 1, 1), DateTime.Now);
            Assert.AreEqual(null, model.Inner);
            Assert.AreEqual(0, model.Inner.Liste.Count);
            Assert.AreEqual(false, model.Inner.Bool);
            
            Assert.AreEqual("", dic[""].Bool);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new QuickAssertsCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new QuickAssertsAnalyzer();
        }
    }

    public class TestClass
    {
        public InnerTestClass Inner { get; set; }
    }

    public class InnerTestClass
    {
        public int Id { get; set; }
        public List<string> Liste { get; set; }
        public bool Bool { get; set; }
    }
}