using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using LayerProcessing;

namespace LayerParserTest
{
    [TestClass]
    public class UnitTestLayerProcessing
    {
        [TestMethod]
        public void AllChanges()
        {
            string str = "ИС_ВС_л_хп_неутв";
            LayerParser lpr = new SimpleLayerParser(str);
            lpr.ExtProjNameAssign("Аникеевка"); Assert.AreEqual("ИС_[Аникеевка]_ВС_л_хп_неутв",lpr.OutputLayerName);
            lpr.ExtProjNameAssign("Опалиха"); Assert.AreEqual("ИС_[Опалиха]_ВС_л_хп_неутв", lpr.OutputLayerName);
            lpr.ExtProjNameAssign("Опалиха"); Assert.AreEqual("ИС_[Опалиха]_ВС_л_хп_неутв", lpr.OutputLayerName);
            lpr.ExtProjNameAssign(""); Assert.AreEqual("ИС_ВС_л_хп_неутв", lpr.OutputLayerName);
            lpr.ExtProjNameAssign(""); Assert.AreEqual("ИС_ВС_л_хп_неутв", lpr.OutputLayerName);
            lpr.StatusSwitch(LayerParser.Status.Existing); Assert.AreEqual("ИС_ВС_л_хп_сущ", lpr.OutputLayerName);
            lpr.ReconstrSwitch(); Assert.AreEqual("ИС_ВС_л_хп_сущ", lpr.OutputLayerName);
            lpr.StatusSwitch(LayerParser.Status.Planned); Assert.AreEqual("ИС_ВС_л_хп_пр", lpr.OutputLayerName);
            lpr.ReconstrSwitch(); Assert.AreEqual("ИС_ВС_л_хп_пр_пер", lpr.OutputLayerName);
            lpr.StatusSwitch(LayerParser.Status.NSPlanned); Assert.AreEqual("ИС_ВС_л_хп_неутв_пер", lpr.OutputLayerName);
        }
        [TestMethod]
        public void ComplexInsideBrackets()
        {
            string str = "ИС_[Пупкино_Залупкино]_ВС_л_хп_неутв";
            LayerParser lpr = new SimpleLayerParser(str);

            lpr.ExtProjNameAssign("Аникеевка"); Assert.AreEqual("ИС_[Аникеевка]_ВС_л_хп_неутв", lpr.OutputLayerName);
            lpr.ExtProjNameAssign("Пыжи_Стрижи"); Assert.AreEqual("ИС_[Пыжи_Стрижи]_ВС_л_хп_неутв", lpr.OutputLayerName);
            lpr.StatusSwitch(LayerParser.Status.Deconstructing); Assert.AreEqual("ИС_ВС_л_хп_дем", lpr.OutputLayerName);

        }
        [TestMethod]
        public void StPrefTest()
        {
            //LayerParser.StandartPrefix="ИС";
            Assert.AreEqual("ИС", LayerParser.StandartPrefix);
        }

        [TestMethod]
        public void TrueNameTest()
        {
            string[] str = new string[3] { "ИС_[Пыжиково]_ЭС_л_КЛ_0.4кВ_неутв", "ИС_ГС_л_распред_0.005_пр_пер", "ИС_ДК_л_закрытая_сущ" };
            LayerParser lp = new SimpleLayerParser(str[0]);
            Assert.AreEqual("ЭС_л_КЛ_0.4кВ_неутв", lp.TrueName);
            LayerParser lp1 = new SimpleLayerParser(str[1]);
            Assert.AreEqual("ГС_л_распред_0.005_пр", lp1.TrueName);
            LayerParser lp2 = new SimpleLayerParser(str[2]);
            Assert.AreEqual("ДК_л_закрытая_сущ", lp2.TrueName);
        }
    }
}
