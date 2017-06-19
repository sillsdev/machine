using System;
using System.IO;
using NUnit.Framework;

namespace SIL.Machine.Morphology.HermitCrab
{
	[TestFixture]
	public class XmlLanguageSerializationTests
	{
		private static string TestXmlFileName
		{
			get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "TestData", "XmlLanguageSerializationTests.xml"); }
		}

		private static string TempXmlFileName
		{
			get { return Path.Combine(Path.GetTempPath(), "XmlLanguageSerializationTests.xml"); }
		}

		[Test]
		public void RoundTripXml()
		{
			try
			{
				Language lang = XmlLanguageLoader.Load(TestXmlFileName);
				XmlLanguageWriter.Save(lang, TempXmlFileName);
				string testXml = File.ReadAllText(TestXmlFileName);
				string tempXml = File.ReadAllText(TempXmlFileName);
				Assert.That(tempXml, Is.EqualTo(testXml));
			}
			finally
			{
				if (File.Exists(TempXmlFileName))
					File.Delete(TempXmlFileName);
			}
		}
	}
}
