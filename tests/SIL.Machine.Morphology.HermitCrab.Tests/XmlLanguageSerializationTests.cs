using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab;

[TestFixture]
public class XmlLanguageSerializationTests
{
    private static string TestXmlFileName =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TestData", "XmlLanguageSerializationTests.xml");

    private static string TempXmlFileName => Path.Combine(Path.GetTempPath(), "XmlLanguageSerializationTests.xml");

    [Test]
    public void RoundTripXml()
    {
        try
        {
            Language lang = XmlLanguageLoader.Load(TestXmlFileName);
            XmlLanguageWriter.Save(lang, TempXmlFileName);
            string testXml = File.ReadAllText(TestXmlFileName).Replace("\r\n", "\n");
            string tempXml = File.ReadAllText(TempXmlFileName).Replace("\r\n", "\n");
            Assert.That(tempXml, Is.EqualTo(testXml));
        }
        finally
        {
            if (File.Exists(TempXmlFileName))
                File.Delete(TempXmlFileName);
        }
    }
}
