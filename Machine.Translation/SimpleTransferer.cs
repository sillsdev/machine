using System.Collections.Generic;

namespace SIL.Machine.Translation
{
	public class SimpleTransferer : ITransferer
	{
		private readonly IMorphemeMapper _morphemeMapper;

		public SimpleTransferer(IMorphemeMapper morphemeMapper)
		{
			_morphemeMapper = morphemeMapper;
		}

		public IEnumerable<WordAnalysis> Transfer(WordAnalysis sourceAnalysis)
		{
			var targetMorphemes = new List<MorphemeInfo>();
			foreach (MorphemeInfo sourceMorpheme in sourceAnalysis.Morphemes)
			{
				MorphemeInfo targetMorpheme;
				if (!_morphemeMapper.TryGetTargetMorpheme(sourceMorpheme, out targetMorpheme))
					yield break;

				targetMorphemes.Add(targetMorpheme);
			}
			yield return new WordAnalysis(targetMorphemes, sourceAnalysis.RootMorphemeIndex, sourceAnalysis.Category);
		}
	}
}
