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
        }
    }
}
