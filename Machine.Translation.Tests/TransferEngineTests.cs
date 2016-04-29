using NSubstitute;
using NUnit.Framework;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Tests
{
	[TestFixture]
	public class TransferEngineTests
	{
		[Test]
		public void Translate_CanTranslate_ReturnsCorrectTranslation()
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
			Assert.That(engine.TranslateWord("habló"), Is.EqualTo("talked".Split(' ')));
		}

		[Test]
		public void Translate_CannotAnalyze_ReturnsEmptyTranslation()
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
			Assert.That(engine.TranslateWord("habló"), Is.Empty);
		}

		[Test]
		public void Translate_CannotGenerate_ReturnsEmptyTranslation()
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
			Assert.That(engine.TranslateWord("habló"), Is.Empty);
		}

		[Test]
		public void Translate_CannotMapMorphemes_ReturnsEmptyTranslation()
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
			Assert.That(engine.TranslateWord("habló"), Is.Empty);
		}
	}
}
