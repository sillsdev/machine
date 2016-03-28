using NSubstitute;
using NUnit.Framework;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Tests
{
	[TestFixture]
	public class TransferEngineTests
	{
		[Test]
		public void TryTranslateWord_CanTranslate_ReturnsTrue()
		{
			var sourceAnalyzer = Substitute.For<ISourceAnalyzer>();
			sourceAnalyzer.AddAnalyses("habló", new WordAnalysis(new[]
			{
				new MorphemeInfo("s1", "v", "talk", MorphemeType.Stem),
				new MorphemeInfo("s2", "v", "pst", MorphemeType.Affix)
			}, 0, "v"));
			var targetGenerator = Substitute.For<ITargetGenerator>();
			var targetMorphemes = new ReadOnlyObservableList<MorphemeInfo>(new ObservableList<MorphemeInfo>
			{
				new MorphemeInfo("e1", "v", "talk", MorphemeType.Stem),
				new MorphemeInfo("e2", "v", "pst", MorphemeType.Affix)
			});
			targetGenerator.Morphemes.Returns(targetMorphemes);
			targetGenerator.AddGeneratedWords(new WordAnalysis(new[] {targetMorphemes[0], targetMorphemes[1]}, 0, "v"), "talked");
			var transferer = new SimpleTransferer(new GlossMorphemeMapper(targetGenerator));
			var engine = new TransferEngine(sourceAnalyzer, transferer, targetGenerator);
			string targetWord;
			Assert.That(engine.TryTranslateWord("habló", out targetWord), Is.True);
			Assert.That(targetWord, Is.EqualTo("talked"));
		}

		[Test]
		public void TryTranslateWord_CannotAnalyze_ReturnsFalse()
		{
			var sourceAnalyzer = Substitute.For<ISourceAnalyzer>();
			sourceAnalyzer.AddAnalyses("habló");
			var targetGenerator = Substitute.For<ITargetGenerator>();
			var targetMorphemes = new ReadOnlyObservableList<MorphemeInfo>(new ObservableList<MorphemeInfo>
			{
				new MorphemeInfo("e1", "v", "talk", MorphemeType.Stem),
				new MorphemeInfo("e2", "v", "pst", MorphemeType.Affix)
			});
			targetGenerator.Morphemes.Returns(targetMorphemes);
			targetGenerator.AddGeneratedWords(new WordAnalysis(new[] {targetMorphemes[0], targetMorphemes[1]}, 0, "v"), "talked");
			var transferer = new SimpleTransferer(new GlossMorphemeMapper(targetGenerator));
			var engine = new TransferEngine(sourceAnalyzer, transferer, targetGenerator);
			string targetWord;
			Assert.That(engine.TryTranslateWord("habló", out targetWord), Is.False);
		}

		[Test]
		public void TryTranslateWord_CannotGenerate_ReturnsFalse()
		{
			var sourceAnalyzer = Substitute.For<ISourceAnalyzer>();
			sourceAnalyzer.AddAnalyses("habló", new WordAnalysis(new[]
			{
				new MorphemeInfo("s1", "v", "talk", MorphemeType.Stem),
				new MorphemeInfo("s2", "v", "pst", MorphemeType.Affix)
			}, 0, "v"));
			var targetGenerator = Substitute.For<ITargetGenerator>();
			var targetMorphemes = new ReadOnlyObservableList<MorphemeInfo>(new ObservableList<MorphemeInfo>
			{
				new MorphemeInfo("e1", "v", "talk", MorphemeType.Stem),
				new MorphemeInfo("e2", "v", "pst", MorphemeType.Affix)
			});
			targetGenerator.Morphemes.Returns(targetMorphemes);
			targetGenerator.AddGeneratedWords(new WordAnalysis(new[] {targetMorphemes[0], targetMorphemes[1]}, 0, "v"));
			var transferer = new SimpleTransferer(new GlossMorphemeMapper(targetGenerator));
			var engine = new TransferEngine(sourceAnalyzer, transferer, targetGenerator);
			string targetWord;
			Assert.That(engine.TryTranslateWord("habló", out targetWord), Is.False);
		}

		[Test]
		public void TryTranslateWord_CannotMapMorphemes_ReturnsFalse()
		{
			var sourceAnalyzer = Substitute.For<ISourceAnalyzer>();
			sourceAnalyzer.AddAnalyses("habló", new WordAnalysis(new[]
			{
				new MorphemeInfo("s1", "v", "talk", MorphemeType.Stem),
				new MorphemeInfo("s2", "v", "pst", MorphemeType.Affix)
			}, 0, "v"));
			var targetGenerator = Substitute.For<ITargetGenerator>();
			var targetMorphemes = new ReadOnlyObservableList<MorphemeInfo>(new ObservableList<MorphemeInfo>());
			targetGenerator.Morphemes.Returns(targetMorphemes);
			var transferer = new SimpleTransferer(new GlossMorphemeMapper(targetGenerator));
			var engine = new TransferEngine(sourceAnalyzer, transferer, targetGenerator);
			string targetWord;
			Assert.That(engine.TryTranslateWord("habló", out targetWord), Is.False);
		}
	}
}
