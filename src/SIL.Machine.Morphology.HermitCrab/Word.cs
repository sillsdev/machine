using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.DataStructures;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
	public class Word : Freezable<Word>, IAnnotatedData<ShapeNode>, ICloneable<Word>
	{
		private readonly Dictionary<string, Allomorph> _allomorphs; 
		private RootAllomorph _rootAllomorph;
		private Shape _shape;
		private readonly List<IMorphologicalRule> _mruleApps;
		private int _mruleAppIndex = -1;
		private readonly Dictionary<IMorphologicalRule, int> _mrulesUnapplied;
		private readonly Dictionary<IMorphologicalRule, int> _mrulesApplied;
		private readonly List<Word> _nonHeadApps;
		private int _nonHeadAppIndex = -1;
		private readonly MprFeatureSet _mprFeatures;
		private readonly IDBearerSet<Feature> _obligatorySyntacticFeatures;
		private FeatureStruct _realizationalFS;
		private Stratum _stratum;
		private bool? _isLastAppliedRuleFinal;
		private bool _isPartial;

		public Word(RootAllomorph rootAllomorph, FeatureStruct realizationalFS)
		{
			_allomorphs = new Dictionary<string, Allomorph>();
			_mprFeatures = new MprFeatureSet();
			_shape = rootAllomorph.Segments.Shape.Clone();
			ResetDirty();
			SetRootAllomorph(rootAllomorph);
			RealizationalFeatureStruct = realizationalFS;
			_mruleApps = new List<IMorphologicalRule>();
			_mrulesUnapplied = new Dictionary<IMorphologicalRule, int>();
			_mrulesApplied = new Dictionary<IMorphologicalRule, int>();
			_nonHeadApps = new List<Word>();
			_obligatorySyntacticFeatures = new IDBearerSet<Feature>();
			_isLastAppliedRuleFinal = null;
		}

		public Word(Stratum stratum, Shape shape)
		{
			_allomorphs = new Dictionary<string, Allomorph>();
			Stratum = stratum;
			_shape = shape;
			ResetDirty();
			SyntacticFeatureStruct = new FeatureStruct();
			RealizationalFeatureStruct = new FeatureStruct();
			_mprFeatures = new MprFeatureSet();
			_mruleApps = new List<IMorphologicalRule>();
			_mrulesUnapplied = new Dictionary<IMorphologicalRule, int>();
			_mrulesApplied = new Dictionary<IMorphologicalRule, int>();
			_nonHeadApps = new List<Word>();
			_obligatorySyntacticFeatures = new IDBearerSet<Feature>();
			_isLastAppliedRuleFinal = null;
			_isPartial = false;
		}

		protected Word(Word word)
		{
			_allomorphs = new Dictionary<string, Allomorph>(word._allomorphs);
			Stratum = word.Stratum;
			_shape = word._shape.Clone();
			_rootAllomorph = word._rootAllomorph;
			SyntacticFeatureStruct = word.SyntacticFeatureStruct.Clone();
			RealizationalFeatureStruct = word.RealizationalFeatureStruct.Clone();
			_mprFeatures = word.MprFeatures.Clone();
			_mruleApps = new List<IMorphologicalRule>(word._mruleApps);
			_mruleAppIndex = word._mruleAppIndex;
			_mrulesUnapplied = new Dictionary<IMorphologicalRule, int>(word._mrulesUnapplied);
			_mrulesApplied = new Dictionary<IMorphologicalRule, int>(word._mrulesApplied);
			_nonHeadApps = new List<Word>(word._nonHeadApps.CloneItems());
			_nonHeadAppIndex = word._nonHeadAppIndex;
			_obligatorySyntacticFeatures = new IDBearerSet<Feature>(word._obligatorySyntacticFeatures);
			_isLastAppliedRuleFinal = word._isLastAppliedRuleFinal;
			_isPartial = word._isPartial;
			CurrentTrace = word.CurrentTrace;
		}

		public IEnumerable<Annotation<ShapeNode>> Morphs
		{
			get
			{
				var morphs = new List<Annotation<ShapeNode>>();
				foreach (Annotation<ShapeNode> ann in Annotations)
				{
					ann.PostorderTraverse(a =>
					{
						if (a.Type() == HCFeatureSystem.Morph)
							morphs.Add(a);
					});
				}
				return morphs;
			}
		}

		public IEnumerable<Allomorph> AllomorphsInMorphOrder
		{
			get { return Morphs.Select(GetAllomorph); }
		}

		public ICollection<Allomorph> Allomorphs
		{
			get { return _allomorphs.Values; }
		}

		public RootAllomorph RootAllomorph
		{
			get { return _rootAllomorph; }

			internal set
			{
				CheckFrozen();
				_shape = value.Segments.Shape.Clone();
				SetRootAllomorph(value);
			}
		}

		private void SetRootAllomorph(RootAllomorph rootAllomorph)
		{
			_rootAllomorph = rootAllomorph;
			var entry = (LexEntry) _rootAllomorph.Morpheme;
			Stratum = entry.Stratum;
			MarkMorph(_shape, _rootAllomorph);
			SyntacticFeatureStruct = entry.SyntacticFeatureStruct.Clone();
			_mprFeatures.Clear();
			_mprFeatures.UnionWith(entry.MprFeatures);
			_isPartial = entry.IsPartial;
		}

		public Shape Shape
		{
			get { return _shape; }
		}

		public FeatureStruct SyntacticFeatureStruct { get; internal set; }

		public FeatureStruct RealizationalFeatureStruct
		{
			get { return _realizationalFS; }
			internal set
			{
				CheckFrozen();
				_realizationalFS = value;
			}
		}

		public MprFeatureSet MprFeatures
		{
			get { return _mprFeatures; }
		}

		public ICollection<Feature> ObligatorySyntacticFeatures
		{
			get { return _obligatorySyntacticFeatures; }
		}

		public Span<ShapeNode> Span
		{
			get { return _shape.Span; }
		}

		public AnnotationList<ShapeNode> Annotations
		{
			get { return _shape.Annotations; }
		}

		public Stratum Stratum
		{
			get { return _stratum; }
			internal set
			{
				CheckFrozen();
				_stratum = value;
			}
		}

		public IEnumerable<Morpheme> MorphemesInApplicationOrder
		{
			get
			{
				yield return _rootAllomorph.Morpheme;
				int j = _nonHeadApps.Count - 1;
				for (int i = _mruleApps.Count - 1; i >= 0; i--)
				{
					IMorphologicalRule rule = _mruleApps[i];
					if (rule == null || rule is CompoundingRule)
						yield return _nonHeadApps[j--].RootAllomorph.Morpheme;
					else
						yield return (MorphemicMorphologicalRule) rule;
				}
			}
		}

		public object CurrentTrace { get; set; }

		public bool IsPartial
		{
			get { return _isPartial; }
			internal set
			{
				CheckFrozen();
				_isPartial = value;
			}
		}

		internal bool IsAllMorphologicalRulesApplied
		{
			get { return _mruleAppIndex == -1; }
		}

		internal bool IsMorphologicalRuleApplicable(IMorphologicalRule rule)
		{
			if (_mruleAppIndex < 0)
				return false;

			IMorphologicalRule curRule = _mruleApps[_mruleAppIndex];
			return curRule == rule || (curRule == null && rule is CompoundingRule);
		}

		internal bool HasRemainingRulesFromStratum(Stratum stratum)
		{
			if (_mruleAppIndex < 0)
				return false;

			IMorphologicalRule curRule = _mruleApps[_mruleAppIndex];
			if (curRule == null)
				return CurrentNonHead != null && CurrentNonHead.Stratum == stratum;
			return curRule.Stratum == stratum;
		}

		internal Annotation<ShapeNode> MarkMorph(IEnumerable<ShapeNode> nodes, Allomorph allomorph)
		{
			ShapeNode[] nodeArray = nodes.ToArray();
			Annotation<ShapeNode> ann = null;
			if (nodeArray.Length > 0)
			{
				ann = new Annotation<ShapeNode>(_shape.SpanFactory.Create(nodeArray[0], nodeArray[nodeArray.Length - 1]), FeatureStruct.New()
					.Symbol(HCFeatureSystem.Morph)
					.Feature(HCFeatureSystem.Allomorph).EqualTo(allomorph.ID).Value);
				ann.Children.AddRange(nodeArray.Select(n => n.Annotation));
				_shape.Annotations.Add(ann, false);
			}
			_allomorphs[allomorph.ID] = allomorph;
			return ann;
		}

		internal Annotation<ShapeNode> MarkSubsumedMorph(Annotation<ShapeNode> morph, Allomorph allomorph)
		{
			Annotation<ShapeNode> ann = new Annotation<ShapeNode>(morph.Span, FeatureStruct.New()
				.Symbol(HCFeatureSystem.Morph)
				.Feature(HCFeatureSystem.Allomorph).EqualTo(allomorph.ID).Value);
			morph.Children.Add(ann, false);
			_allomorphs[allomorph.ID] = allomorph;
			return ann;
		}

		internal void RemoveMorph(Annotation<ShapeNode> morphAnn)
		{
			var alloID = (string) morphAnn.FeatureStruct.GetValue(HCFeatureSystem.Allomorph);
			_allomorphs.Remove(alloID);
			foreach (ShapeNode node in _shape.GetNodes(morphAnn.Span).ToArray())
				node.Remove();
		}

		/// <summary>
		/// Notifies this word that the specified morphological rule was unapplied. Null
		/// indicates that an unknown compounding rule was unapplied. This is used when
		/// generating a compound word, because the compounding rule is usually not known just
		/// the non-head allomorph. 
		/// </summary>
		internal void MorphologicalRuleUnapplied(IMorphologicalRule mrule)
		{
			CheckFrozen();
			if (mrule != null)
				_mrulesUnapplied.UpdateValue(mrule, () => 0, count => count + 1);
			if (!(mrule is RealizationalAffixProcessRule))
			{
				_mruleApps.Add(mrule);
				_mruleAppIndex++;
			}
		}

		/// <summary>
		/// Gets the number of times the specified morphological rule has been unapplied.
		/// </summary>
		/// <param name="mrule">The morphological rule.</param>
		/// <returns>The number of unapplications.</returns>
		internal int GetUnapplicationCount(IMorphologicalRule mrule)
		{
			int numUnapplies;
			if (!_mrulesUnapplied.TryGetValue(mrule, out numUnapplies))
				numUnapplies = 0;
			return numUnapplies;
		}

		/// <summary>
		/// Notifies this word synthesis that the specified morphological rule has applied.
		/// </summary>
		internal void MorphologicalRuleApplied(IMorphologicalRule mrule)
		{
			CheckFrozen();
			if (IsMorphologicalRuleApplicable(mrule))
				_mruleAppIndex--;
			// indicate that the current non-head was applied if this is a compounding rule
			if (mrule is CompoundingRule)
				_nonHeadAppIndex--;
			_mrulesApplied.UpdateValue(mrule, () => 0, count => count + 1);
		}

		internal bool? IsLastAppliedRuleFinal
		{
			get { return _isLastAppliedRuleFinal; }
			set
			{
				CheckFrozen();
				_isLastAppliedRuleFinal = value;
			}
		}

		/// <summary>
		/// Gets the number of times the specified morphological rule has been applied.
		/// </summary>
		/// <param name="mrule">The morphological rule.</param>
		/// <returns>The number of applications.</returns>
		internal int GetApplicationCount(IMorphologicalRule mrule)
		{
			int numApplies;
			if (!_mrulesApplied.TryGetValue(mrule, out numApplies))
				numApplies = 0;
			return numApplies;
		}

		internal Word CurrentNonHead
		{
			get
			{
				if (_nonHeadAppIndex == -1)
					return null;
				return _nonHeadApps[_nonHeadAppIndex];
			}
		}

		internal int NonHeadCount
		{
			get { return _nonHeadApps.Count; }
		}

		internal void NonHeadUnapplied(Word nonHead)
		{
			CheckFrozen();
			_nonHeadApps.Add(nonHead);
			_nonHeadAppIndex++;
		}

		public Allomorph GetAllomorph(Annotation<ShapeNode> morph)
		{
			var alloID = (string) morph.FeatureStruct.GetValue(HCFeatureSystem.Allomorph);
			return _allomorphs[alloID];
		}

		internal bool CheckBlocking(out Word word)
		{
			word = null;
			LexFamily family = ((LexEntry) RootAllomorph.Morpheme).Family;
			if (family == null)
				return false;

			foreach (LexEntry entry in family.Entries)
			{
				if (entry != RootAllomorph.Morpheme && entry.Stratum == Stratum && SyntacticFeatureStruct.Subsumes(entry.SyntacticFeatureStruct))
				{
					word = new Word(entry.PrimaryAllomorph, RealizationalFeatureStruct.Clone()) { CurrentTrace = CurrentTrace };
					word.Freeze();
					return true;
				}
			}

			return false;
		}

		internal void ResetDirty()
		{
			CheckFrozen();
			foreach (ShapeNode node in _shape)
				node.SetDirty(false);
		}

		internal IDictionary<int, Tuple<FailureReason, object>> CurrentRuleResults { get; set; }

		protected override int FreezeImpl()
		{
			int code = 23;
			_shape.Freeze();
			code = code * 31 + _shape.GetFrozenHashCode();
			_realizationalFS.Freeze();
			code = code * 31 + _realizationalFS.GetFrozenHashCode();
			foreach (Word nonHead in _nonHeadApps)
			{
				nonHead.Freeze();
				code = code * 31 + nonHead.GetFrozenHashCode();
			}
			code = code * 31 + _nonHeadAppIndex.GetHashCode();
			code = code * 31 + _stratum.GetHashCode();
			code = code * 31 + (_rootAllomorph == null ? 0 : _rootAllomorph.GetHashCode());
			code = code * 31 + _mruleApps.GetSequenceHashCode();
			code = code * 31 + _mruleAppIndex.GetHashCode();
			code = code * 31 + _isLastAppliedRuleFinal.GetHashCode();
			return code;
		}

		public override bool ValueEquals(Word other)
		{
			if (other == null)
				return false;

			if (IsFrozen && other.IsFrozen && GetFrozenHashCode() != other.GetFrozenHashCode())
				return false;

			return _shape.ValueEquals(other._shape) && _realizationalFS.ValueEquals(other._realizationalFS)
				&& _nonHeadApps.SequenceEqual(other._nonHeadApps, FreezableEqualityComparer<Word>.Default) && _nonHeadAppIndex == other._nonHeadAppIndex
				&& _stratum == other._stratum && _rootAllomorph == other._rootAllomorph && _mruleApps.SequenceEqual(other._mruleApps)
				&& _mruleAppIndex == other._mruleAppIndex && _isLastAppliedRuleFinal == other._isLastAppliedRuleFinal;
		}

		public Word Clone()
		{
			return new Word(this);
		}

		public override string ToString()
		{
			return Shape.ToRegexString(Stratum.CharacterDefinitionTable, true);
		}
	}
}
