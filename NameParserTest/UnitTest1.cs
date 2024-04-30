using System.Reflection;
using System.Xml.Linq;

namespace NameParserTest
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            FileInfo fi = new FileInfo(Assembly.GetExecutingAssembly().Location);
            XDocument xDocument = XDocument.Load(Path.Combine(fi.Directory!.FullName, "TestData", "LayerParserTemplate.xml"));
            _ = new NameParser(xDocument);
        }
        [Test]
        public void Test1()
        {
            NameParser? parser = NameParser.LoadedParsers.Values.First();

            Assert.That(parser, Is.Not.Null);
            Assert.That(parser.Prefix, Is.EqualTo("ÈÑ"));

            LayerInfo layerInfo = parser.GetLayerInfo("ÈÑ_ÝÑ_ë_ÊË_0.4êÂ_ïð");
            Assert.That(layerInfo, Is.Not.Null);

            Assert.Pass();
        }
    }
}