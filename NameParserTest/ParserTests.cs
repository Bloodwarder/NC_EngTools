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
            string path = Path.Combine(fi.Directory!.FullName, "TestData", "LayerParser_��.xml");
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
            Assert.That(_parser.Prefix, Is.EqualTo("��"));
        }

        [Test]
        public void LayerInfoTestWhenProper()
        {
            _layerInfo = _parser!.GetLayerInfo("��_��_�_��_0.4��_��").Value;
            Assert.That(_layerInfo, Is.Not.Null);
            _layerInfo.ChangeAuxilaryData("ExternalProject", "���-2017");
            Assert.That(_layerInfo.Name, Is.EqualTo("��_[���-2017]_��_�_��_0.4��_�����"));
            _layerInfo.ChangeAuxilaryData("ExternalProject", null);
            Assert.That(_layerInfo.Name, Is.EqualTo("��_��_�_��_0.4��_�����"));
            _layerInfo.SuffixTagged["Reconstruction"] = true;
            Assert.That(_layerInfo.Name, Is.EqualTo("��_��_�_��_0.4��_�����_���"));
        }
        [Test]
        public void LayerInfoWhenWrongShouldThrow()
        {
            try
            {
                _ = _parser!.GetLayerInfo("���_���44_�����341");
            }
            catch (Exception ex)
            {
                Assert.That(ex, Is.InstanceOf<WrongLayerException>());
                return;
            }
            Assert.Fail("�� ��������� ����������");
        }

        [Test]
        public void LayerInfoWhenSwitchStatusShouldDiscardAuxData()
        {
            _layerInfo = _parser!.GetLayerInfo("��_[������ - �-7]_��_�_�������_0.6_�����").Value;
            string? aux = _layerInfo.AuxilaryData["ExternalProject"];
            Assert.That(aux, Is.EqualTo("������ - �-7"));
            _layerInfo.SwitchStatus("��");
            Assert.That(_layerInfo.Status, Is.EqualTo("��"));
            Assert.That(_layerInfo.AuxilaryData["ExternalProject"], Is.Null);
            Assert.That(_layerInfo.Name, Is.EqualTo("��_��_�_�������_0.6_��"));
        }

        [Test]
        public void LayerInfoWhenSwitchStatusShouldDiscardSuffix()
        {
            _layerInfo = _parser!.GetLayerInfo("��_[������ - �-7]_��_�_�������_0.6_�����_���").Value;
            bool? tagged = _layerInfo.SuffixTagged["Reconstruction"];
            Assert.That(tagged, Is.EqualTo(true));
            _layerInfo.SwitchStatus("��");
            Assert.That(_layerInfo.Status, Is.EqualTo("��"));
            Assert.That(_layerInfo.SuffixTagged["Reconstruction"], Is.EqualTo(true));

            _layerInfo.SwitchStatus("���");
            Assert.That(_layerInfo.SuffixTagged["Reconstruction"], Is.EqualTo(false));
            _layerInfo.SwitchSuffix("Reconstruction", true);
            Assert.That(_layerInfo.Status, Is.EqualTo("��"));

            _layerInfo.SwitchStatus("���");
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
                _ = _parser!.GetLayerInfo("��_�����-��_���������_[�����]_���");
            }
            catch (Exception ex)
            {
                Assert.That(ex, Is.InstanceOf<WrongLayerException>());
                return;
            }
            Assert.Fail("�� ��������� ����������");
        }

    }
}