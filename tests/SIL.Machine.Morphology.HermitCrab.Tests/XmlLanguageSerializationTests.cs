using NUnit.Framework;
using System.Diagnostics;

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

    [Test]
    public void TestParseWord()
    {
        Language language = XmlLanguageLoader.Load("C:\\Users\\PC\\AppData\\Local\\Temp\\Mbugwe LizzieHC practice.xml");
        Morpher morpher = new Morpher(new TraceManager(), language);
        var output = morpher.ParseWord("wa", out _);
        Debug.WriteLine(output);
    }

}
