using NameClassifiers.Filters;
using NameClassifiers.SharedProperties;
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
    internal class SerializationTests
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
        public void FilterInitialTest()
        {
            var element = XDocument.Load(_path).Element("LayerParser")?.Element("LegendFilters");
            if (element == null)
                Assert.Fail("Не найден корневой Xml элемент");

            XmlSerializer serializer = new(typeof(GlobalFilters));
            using (XmlReader reader = element!.CreateReader())
            {
                GlobalFilters? globalFilters = serializer.Deserialize(reader) as GlobalFilters;
                Assert.That(globalFilters, Is.Not.Null);
                Assert.That(globalFilters.Sections, Is.Not.Null);
                Assert.That(globalFilters.Sections.FirstOrDefault()?.Filters, Is.Not.Null);
                Assert.That(globalFilters.Sections.FirstOrDefault()?.Filters.FirstOrDefault()?.Grids, Is.InstanceOf<IEnumerable<GridFilter>>());
            }
        }

        [Test]
        public void SharedPropertiesInitialTest()
        {
            var element = XDocument.Load(_path).Element("LayerParser")?.Element("SharedProperties");
            if (element == null)
                Assert.Fail("Не найден корневой Xml элемент");
            XmlSerializer serializer = new(typeof(SharedPropertiesCollection));
            serializer.UnknownElement += Serializer_UnknownElement;
            using (XmlReader reader = element!.CreateReader())
            {
                SharedPropertiesCollection? sharedProperties = serializer.Deserialize(reader) as SharedPropertiesCollection;
                Assert.That(sharedProperties, Is.Not.Null);
                Assert.That(sharedProperties.Properties, Is.Not.Null);
                Assert.That(sharedProperties.Properties.FirstOrDefault(), Is.Not.Null);
                Assert.That(sharedProperties.Properties[1].Groups[1].DefaultValue?.Value, Is.InstanceOf<Color>());
            }
        }

        private void Serializer_UnknownElement(object? sender, XmlElementEventArgs e)
        {
            string expected = e.ExpectedElements;
            string actual = e.Element.LocalName;
        }

        [Test]
        public void ColorSerializationTest()
        {
            XDocument doc = XDocument.Load(_path);
            var element = doc.Root?.Element("SharedProperties")?
                                   .Elements("Property")?
                                   .Where(e => e.Attribute("Name")?.Value == "Color")
                                   .FirstOrDefault()?
                                   .Elements()?
                                   .ElementAt(1)?
                                   .Element("DefaultValue")?
                                   .Element("Color");
            if (element == null)
                Assert.Fail("Не найден корневой Xml элемент");
            XmlSerializer serializer = new(typeof(Color));
            serializer.UnknownElement += Serializer_UnknownElement;
            using (XmlReader reader = element!.CreateReader())
            {
                Color color = (Color)serializer.Deserialize(reader)!;
                Assert.That(color.Red, Is.EqualTo(107));
            }
        }
    }
}
