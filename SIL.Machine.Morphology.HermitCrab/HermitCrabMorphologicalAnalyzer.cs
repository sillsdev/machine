using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
	public class HermitCrabMorphologicalAnalyzer : IMorphologicalAnalyzer
	{
		private readonly Morpher _morpher;
		private readonly Func<Morpheme, string> _getMorphemeId;
		private readonly Func<FeatureStruct, string> _getCategory; 

		public HermitCrabMorphologicalAnalyzer(Func<Morpheme, string> getMorphemeId, Func<FeatureStruct, string> getCategory, Morpher morpher)
		{
			_getMorphemeId = getMorphemeId;
			_getCategory = getCategory;
			_morpher = morpher;
		}

		public IEnumerable<WordAnalysis> AnalyzeWord(string word)
		{
			try
			{
				return _morpher.ParseWord(word).Select(CreateWordAnalysis);
			}
			catch (InvalidShapeException)
			{
				return Enumerable.Empty<WordAnalysis>();
			}
		}

		private WordAnalysis CreateWordAnalysis(Word result)
		{
			int rootMorphemeIndex = -1;
			var morphemes = new List<Morphology.Morpheme>();
			int i = 0;
			foreach (Allomorph allo in result.AllomorphsInMorphOrder)
			{
				morphemes.Add(CreateMorphemeInfo(allo.Morpheme));
				if (allo == result.RootAllomorph)
					rootMorphemeIndex = i;
				i++;
			}
			return new WordAnalysis(morphemes, rootMorphemeIndex, _getCategory(result.SyntacticFeatureStruct));
		}

		private Morphology.Morpheme CreateMorphemeInfo(Morpheme morpheme)
		{
			string id = _getMorphemeId(morpheme);
			string category;
			MorphemeType morphemeType;
			var entry = morpheme as LexEntry;
			if (entry != null)
			{
				category = _getCategory(entry.SyntacticFeatureStruct);
				morphemeType = MorphemeType.Stem;
			}
			else
			{
				morphemeType = MorphemeType.Affix;
				var apr = morpheme as AffixProcessRule;
				if (apr != null)
				{
					category = _getCategory(apr.OutSyntacticFeatureStruct) ?? _getCategory(apr.RequiredSyntacticFeatureStruct);
				}
				else
				{
					var rapr = (RealizationalAffixProcessRule) morpheme;
					category = _getCategory(rapr.RealizationalFeatureStruct) ?? _getCategory(rapr.RequiredSyntacticFeatureStruct);
				}
			}
			return new Morphology.Morpheme(id, category, morpheme.Gloss, morphemeType);
		}
	}
}
