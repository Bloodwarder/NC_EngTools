using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExternalData;
using System;

namespace LPropsTest
{
    [TestClass]
    public class LPropsTests
    {
        [TestMethod]
        public void DataExtractionTest()
        {
            LayerProps lp = LayerPropertiesDictionary.GetLayerProps("ВС_л_хп_неутв");
            Assert.AreEqual(lp.ConstWidth, 0.6);
            Assert.AreEqual(lp.LTScale, 0.8);
            Assert.AreEqual(lp.Green, 0);
        }
    }
}
