using NameClassifiers.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace NameParserTest
{
    [TestFixture]
    internal class FilterSerializationTests
    {
        string _path;

        [OneTimeSetUp]
        public void SetUp()
        {
            FileInfo fi = new(Assembly.GetExecutingAssembly().Location);
            _path = Path.Combine(fi.Directory!.FullName, "TestData", "LayerParserTemplate.xml");
        }
        [OneTimeTearDown]
        public void TearDown()
        {

        }

        [Test]
        public void InitialTest()
        {

            XmlSerializer serializer = new(typeof(GlobalFilters));

            var element = XDocument.Load(_path).Element("LayerParser")?.Element("LegendFilters");
            if (element == null)
                Assert.Fail("Не найден корневой Xml элемент");
            using (XmlReader reader = element!.CreateReader())
            {
                GlobalFilters? globalFilters = serializer.Deserialize(reader) as GlobalFilters;
                Assert.That(globalFilters, Is.Not.Null);
                Assert.That(globalFilters.Sections, Is.Not.Null);
                Assert.That(globalFilters.Sections.FirstOrDefault()?.Filters, Is.Not.Null);
                Assert.That(globalFilters.Sections.FirstOrDefault()?.Filters.FirstOrDefault()?.Grids.FirstOrDefault(), Is.Not.Null);
            }

        }





        [Test]
        public void ElementsProbeTest()
        {
            Assert.Fail();
        }



    }
}
