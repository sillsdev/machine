using SIL.Machine.Tokenization;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;

namespace SIL.Machine.Corpora
{
	public class DblBundleTextCorpus : DictionaryTextCorpus
	{
		public DblBundleTextCorpus(ITokenizer<string, int> wordTokenizer, string fileName)
			: base(GetTexts(wordTokenizer, fileName))
		{
		}

		private static IEnumerable<IText> GetTexts(ITokenizer<string, int> wordTokenizer, string fileName)
		{
			using (ZipArchive archive = ZipFile.OpenRead(fileName))
			{
				ZipArchiveEntry metadataEntry = archive.GetEntry("metadata.xml");
				using (Stream stream = metadataEntry.Open())
				{
					var doc = XDocument.Load(stream);
					if ((string) doc.Root.Attribute("version") != "2.0")
						throw new InvalidOperationException("");

					foreach (XElement contentElem in doc.Root.Elements("publications").Elements("publication")
						.Elements("structure").Elements("content"))
					{
						yield return new DblBundleText(wordTokenizer, (string) contentElem.Attribute("role"), fileName,
							(string) contentElem.Attribute("src"));
					}
				}
			}
		}
	}
}
