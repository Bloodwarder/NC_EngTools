using System.Reflection;

namespace NameParserTest
{
    [TestFixture]
    public class ParserTests
    {
        LayerInfo? _layerInfo;
        NameParser? _parser;

        [OneTimeSetUp]
        public void Setup()
        {
            FileInfo fi = new(Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(fi.Directory!.FullName, "TestData", "LayerParser_ИС.xml");
            _parser = NameParser.Load(path);
        }
        [OneTimeTearDown]
        public void TearDown()
        {
            _layerInfo = null;
        }
        [Test]
        public void ParserInitializationTestWhenProper()
        {
            Assert.That(_parser, Is.Not.Null);
            Assert.That(_parser.Prefix, Is.EqualTo("ИС"));
        }

        [Test]
        public void LayerInfoTestWhenProper()
        {
            _layerInfo = _parser!.GetLayerInfo("ИС_ЭС_л_КЛ_0.4кВ_пр").Value;
            Assert.That(_layerInfo, Is.Not.Null);
            _layerInfo.ChangeAuxilaryData("ExternalProject", "ВСМ-2017");
            Assert.That(_layerInfo.Name, Is.EqualTo("ИС_[ВСМ-2017]_ЭС_л_КЛ_0.4кВ_неутв"));
            _layerInfo.ChangeAuxilaryData("ExternalProject", null);
            Assert.That(_layerInfo.Name, Is.EqualTo("ИС_ЭС_л_КЛ_0.4кВ_неутв"));
            _layerInfo.SuffixTagged["Reconstruction"] = true;
            Assert.That(_layerInfo.Name, Is.EqualTo("ИС_ЭС_л_КЛ_0.4кВ_неутв_пер"));
        }
        [Test]
        public void LayerInfoWhenWrongShouldThrow()
        {
            try
            {
                _ = _parser!.GetLayerInfo("вап_ыык44_аклвю341");
            }
            catch (Exception ex)
            {
                Assert.That(ex, Is.InstanceOf<WrongLayerException>());
                return;
            }
            Assert.Fail("Не выброшено исключение");
        }

        [Test]
        public void LayerInfoWhenSwitchStatusShouldDiscardAuxData()
        {
            _layerInfo = _parser!.GetLayerInfo("ИС_[Кучино - М-7]_ГС_л_распред_0.6_неутв").Value;
            string? aux = _layerInfo.AuxilaryData["ExternalProject"];
            Assert.That(aux, Is.EqualTo("Кучино - М-7"));
            _layerInfo.SwitchStatus("пр");
            Assert.That(_layerInfo.Status, Is.EqualTo("пр"));
            Assert.That(_layerInfo.AuxilaryData["ExternalProject"], Is.Null);
            Assert.That(_layerInfo.Name, Is.EqualTo("ИС_ГС_л_распред_0.6_пр"));
        }

        [Test]
        public void LayerInfoWhenSwitchStatusShouldDiscardSuffix()
        {
            _layerInfo = _parser!.GetLayerInfo("ИС_[Кучино - М-7]_ГС_л_распред_0.6_неутв_пер").Value;
            bool? tagged = _layerInfo.SuffixTagged["Reconstruction"];
            Assert.That(tagged, Is.EqualTo(true));
            _layerInfo.SwitchStatus("пр");
            Assert.That(_layerInfo.Status, Is.EqualTo("пр"));
            Assert.That(_layerInfo.SuffixTagged["Reconstruction"], Is.EqualTo(true));

            _layerInfo.SwitchStatus("сущ");
            Assert.That(_layerInfo.SuffixTagged["Reconstruction"], Is.EqualTo(false));
            _layerInfo.SwitchSuffix("Reconstruction", true);
            Assert.That(_layerInfo.Status, Is.EqualTo("пр"));

            _layerInfo.SwitchStatus("дем");
            try
            {
                _layerInfo.SwitchSuffix("Reconstruction", true);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.That(ex, Is.InstanceOf<WrongLayerException>());
            }
        }


        [Test]
        public void LayerInfoWhenProperPrefixWrongInfoShouldThrow()
        {
            try
            {
                _ = _parser!.GetLayerInfo("ИС_какая-то_рандомная_[хрень]_пер");
            }
            catch (Exception ex)
            {
                Assert.That(ex, Is.InstanceOf<WrongLayerException>());
                return;
            }
            Assert.Fail("Не выброшено исключение");
        }

    }
}