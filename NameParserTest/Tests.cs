using System.Reflection;
using System.Xml.Linq;

namespace NameParserTest
{
    [TestFixture]
    public class Tests
    {
        XDocument? _xDocument;
        LayerInfo? _layerInfo;
        NameParser? _parser;

        [OneTimeSetUp]
        public void Setup()
        {
            FileInfo fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var path = Path.Combine(fi.Directory!.FullName, "TestData", "LayerParserTemplate.xml");
            _xDocument = XDocument.Load(path);
            _parser = NameParser.Load(path);
        }
        [OneTimeTearDown]
        public void TearDown()
        {
            _layerInfo = null;
            _xDocument = null;
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
            _layerInfo = _parser!.GetLayerInfo("��_��_�_��_0.4��_��");
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
        public void LayerInfoWhenProperPrefixWrongInfoShouldThrow()
        {
            try
            {
                _ = _parser.GetLayerInfo("��_�����-��_���������_[�����]_���");
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