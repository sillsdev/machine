using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.Machine.Tokenization;
using SIL.ObjectModel;

namespace SIL.Machine.Corpora
{
	[TestFixture]
	public class ParatextBackupTextCorpusTests
	{
		[Test]
		public void Texts()
		{
			using var env = new TestEnvironment();
			Assert.That(env.Corpus.Texts.Select(t => t.Id), Is.EquivalentTo(new[] { "MAT", "MRK" }));
		}

		[Test]
		public void GetText()
		{
			using var env = new TestEnvironment();
			Assert.That(env.Corpus.GetText("MAT").GetSegments(), Is.Not.Empty);
			Assert.That(env.Corpus.GetText("LUK").GetSegments(), Is.Empty);
		}

		private class TestEnvironment : DisposableBase
		{
			private readonly string _backupPath;

			public TestEnvironment()
			{
				_backupPath = CorporaTestHelpers.CreateTestParatextBackup();
				Corpus = new ParatextBackupTextCorpus(new LatinWordTokenizer(), _backupPath);
			}

			public ParatextBackupTextCorpus Corpus { get; }

			protected override void DisposeManagedResources()
			{
				if (File.Exists(_backupPath))
					File.Delete(_backupPath);
			}
		}
	}
}
