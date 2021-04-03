using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using DuplicateHider;

namespace SourceSelectorTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var names = SourceSelector.GetResourceNames();
            var ubisoft = SourceSelector.GetResourceIconUri("ubisoft connect");
            var steam = SourceSelector.GetResourceIconUri("steam");
            var asm = System.Reflection.Assembly.GetAssembly(typeof(DuplicateHider.DuplicateHiderPlugin)).GetName();
            var codebase = asm.EscapedCodeBase;
            var uri = new Uri($"pack://application:,,,/{asm.Name};component/icons/undefined.ico");
        }
    }
}
