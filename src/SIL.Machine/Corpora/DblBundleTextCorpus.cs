using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using SIL.IO;
using SIL.Machine.Tokenization;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
	public class DblBundleTextCorpus : ScriptureTextCorpus
	{
		private static readonly HashSet<string> SupportedVersions = new HashSet<string> { "2.0", "2.1", "2.2" };

		public DblBundleTextCorpus(ITokenizer<string, int, string> wordTokenizer, string fileName) : base(wordTokenizer)
		{
			using (ZipArchive archive = ZipFile.OpenRead(fileName))
			{
				ZipArchiveEntry metadataEntry = archive.GetEntry("metadata.xml");
				using (Stream stream = metadataEntry.Open())
				{
					var doc = XDocument.Load(stream);
					var version = (string)doc.Root.Attribute("version");
					string[] parts = version.Split(new[] { '.' }, 3);
					if (!SupportedVersions.Contains($"{parts[0]}.{parts[1]}"))
						throw new InvalidOperationException("Unsupported version of DBL bundle.");

					ZipArchiveEntry versificationEntry = archive.Entries
						.FirstOrDefault(e => e.Name == "versification.vrs");
					if (versificationEntry != null)
					{
						using (var tempFile = TempFile.CreateAndGetPathButDontMakeTheFile())
						{
							versificationEntry.ExtractToFile(tempFile.Path);
							var abbr = (string)doc.Root.Elements("identification").Elements("abbreviation")
								.FirstOrDefault();
							Versification = Scripture.Versification.Table.Implementation.Load(tempFile.Path, abbr);
						}
					}

					foreach (XElement contentElem in doc.Root.Elements("publications").Elements("publication")
						.Where(pubElem => (bool?)pubElem.Attribute("default") ?? false).Elements("structure")
						.Elements("content"))
					{
						AddText(new UsxZipText(wordTokenizer, (string)contentElem.Attribute("role"), fileName,
							(string)contentElem.Attribute("src"), Versification));
					}
				}
			}
		}

		public override ScrVers Versification { get; } = ScrVers.English;
	}
}
