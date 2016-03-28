using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.FeatureModel;
using SIL.Machine.HermitCrab;
using SIL.Machine.HermitCrab.MorphologicalRules;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.HermitCrab
{
	public class HermitCrabTargetGenerator : ITargetGenerator
	{
		private readonly ReadOnlyObservableCollection<MorphemeInfo> _morphemes;
		private readonly Morpher _morpher;
		private readonly Dictionary<string, Morpheme> _hcMorphemes; 

		public HermitCrabTargetGenerator(Func<Morpheme, string> getMorphemeId, Func<FeatureStruct, string> getCategory, Morpher morpher)
		{
			_morpher = morpher;
			var morphemes = new ObservableList<MorphemeInfo>();
			_hcMorphemes = new Dictionary<string, Morpheme>();
			foreach (Stratum stratum in _morpher.Language.Strata)
			{
				foreach (LexEntry entry in stratum.Entries)
				{
					string id = getMorphemeId(entry);
					morphemes.Add(CreateMorpheme(getCategory, id, entry));
					_hcMorphemes[id] = entry;
				}

				foreach (AffixProcessRule apr in stratum.MorphologicalRules.OfType<AffixProcessRule>())
				{
					string id = getMorphemeId(apr);
					morphemes.Add(CreateMorpheme(getCategory, id, apr));
					_hcMorphemes[id] = apr;
				}
				foreach (Morpheme rule in stratum.AffixTemplates.SelectMany(t => t.Slots).SelectMany(s => s.Rules).OfType<Morpheme>())
				{
					string id = getMorphemeId(rule);
					var apr = rule as AffixProcessRule;
					morphemes.Add(apr != null ? CreateMorpheme(getCategory, id, apr)
						: CreateMorpheme(getCategory, id, (RealizationalAffixProcessRule) rule));
					_hcMorphemes[id] = rule;
				}
			}

			_morphemes = new ReadOnlyObservableCollection<MorphemeInfo>(morphemes);
		}

		private static MorphemeInfo CreateMorpheme(Func<FeatureStruct, string> getCategory, string id, LexEntry entry)
		{
			return new MorphemeInfo(id, getCategory(entry.SyntacticFeatureStruct), entry.Gloss, MorphemeType.Stem);
		}

		private static MorphemeInfo CreateMorpheme(Func<FeatureStruct, string> getCategory, string id, AffixProcessRule apr)
		{
			string category = getCategory(apr.OutSyntacticFeatureStruct) ?? getCategory(apr.RequiredSyntacticFeatureStruct);
			return new MorphemeInfo(id, category, apr.Gloss, MorphemeType.Affix);
		}

		private static MorphemeInfo CreateMorpheme(Func<FeatureStruct, string> getCategory, string id, RealizationalAffixProcessRule rapr)
		{
			string category = getCategory(rapr.RealizationalFeatureStruct) ?? getCategory(rapr.RequiredSyntacticFeatureStruct);
			return new MorphemeInfo(id, category, rapr.Gloss, MorphemeType.Affix);
		}

		public IReadOnlyObservableCollection<MorphemeInfo> Morphemes
		{
			get { return _morphemes; }
		}

		public IEnumerable<string> GenerateWords(WordAnalysis wordAnalysis)
		{
			if (wordAnalysis.Morphemes.Count == 0)
				return Enumerable.Empty<string>();

			var morphemes = new List<Morpheme>();
			foreach (MorphemeInfo mi in wordAnalysis.Morphemes)
			{
				Morpheme morpheme;
				if (!_hcMorphemes.TryGetValue(mi.Id, out morpheme))
					return Enumerable.Empty<string>();
				morphemes.Add(morpheme);
			}

			var rootEntry = (LexEntry) morphemes[wordAnalysis.RootMorphemeIndex];
			var realizationalFS = new FeatureStruct();
			var results = new HashSet<string>();
			foreach (Stack<Morpheme> otherMorphemes in PermuteOtherMorphemes(morphemes, wordAnalysis.RootMorphemeIndex - 1, wordAnalysis.RootMorphemeIndex + 1))
				results.UnionWith(_morpher.GenerateWords(rootEntry, otherMorphemes, realizationalFS));
			return results;
		}

		private IEnumerable<Stack<Morpheme>> PermuteOtherMorphemes(List<Morpheme> morphemes, int leftIndex, int rightIndex)
		{
			if (leftIndex == -1 && rightIndex == morphemes.Count)
			{
				yield return new Stack<Morpheme>();
			}
			else
			{
				if (rightIndex < morphemes.Count)
				{
					foreach (Stack<Morpheme> p in PermuteOtherMorphemes(morphemes, leftIndex, rightIndex + 1))
					{
						p.Push(morphemes[rightIndex]);
						yield return p;
					}
				}

				if (leftIndex > -1)
				{
					foreach (Stack<Morpheme> p in PermuteOtherMorphemes(morphemes, leftIndex - 1, rightIndex))
					{
						p.Push(morphemes[leftIndex]);
						yield return p;
					}
				}
			}
		}
	}
}
