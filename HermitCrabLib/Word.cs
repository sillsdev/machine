using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
	public class Word : Freezable<Word>, IData<ShapeNode>, IDeepCloneable<Word>
	{
		private readonly IDBearerSet<Allomorph> _allomorphs; 
		private RootAllomorph _rootAllomorph;
		private Shape _shape;
		private readonly Stack<IMorphologicalRule> _mrules;
		private readonly Dictionary<IMorphologicalRule, int> _mrulesUnapplied;
		private readonly Dictionary<IMorphologicalRule, int> _mrulesApplied;
		private readonly Stack<Word> _nonHeads;
		private readonly MprFeatureSet _mprFeatures;
		private readonly IDBearerSet<Feature> _obligatorySyntacticFeatures;
		private FeatureStruct _realizationalFS;
		private Stratum _stratum;

		public Word(RootAllomorph rootAllomorph, FeatureStruct realizationalFS)
		{
			_allomorphs = new IDBearerSet<Allomorph>();
			_mprFeatures = new MprFeatureSet();
			_shape = rootAllomorph.Shape.DeepClone();
			ResetDirty();
			SetRootAllomorph(rootAllomorph);
			RealizationalFeatureStruct = realizationalFS;
			_mrules = new Stack<IMorphologicalRule>();
			_mrulesUnapplied = new Dictionary<IMorphologicalRule, int>();
			_mrulesApplied = new Dictionary<IMorphologicalRule, int>();
			_nonHeads = new Stack<Word>();
			_obligatorySyntacticFeatures = new IDBearerSet<Feature>();
		}

		public Word(Stratum stratum, Shape shape)
		{
			_allomorphs = new IDBearerSet<Allomorph>();
			Stratum = stratum;
			_shape = shape;
			ResetDirty();
			SyntacticFeatureStruct = new FeatureStruct();
			RealizationalFeatureStruct = new FeatureStruct();
			_mprFeatures = new MprFeatureSet();
			_mrules = new Stack<IMorphologicalRule>();
			_mrulesUnapplied = new Dictionary<IMorphologicalRule, int>();
			_mrulesApplied = new Dictionary<IMorphologicalRule, int>();
			_nonHeads = new Stack<Word>();
			_obligatorySyntacticFeatures = new IDBearerSet<Feature>();
		}

		protected Word(Word word)
		{
			_allomorphs = new IDBearerSet<Allomorph>(word._allomorphs);
			Stratum = word.Stratum;
			_shape = word._shape.DeepClone();
			_rootAllomorph = word._rootAllomorph;
			SyntacticFeatureStruct = word.SyntacticFeatureStruct.DeepClone();
			RealizationalFeatureStruct = word.RealizationalFeatureStruct.DeepClone();
			_mprFeatures = word.MprFeatures.DeepClone();
			_mrules = new Stack<IMorphologicalRule>(word._mrules.Reverse());
			_mrulesUnapplied = new Dictionary<IMorphologicalRule, int>(word._mrulesUnapplied);
			_mrulesApplied = new Dictionary<IMorphologicalRule, int>(word._mrulesApplied);
			_nonHeads = new Stack<Word>(word._nonHeads.Reverse().DeepClone());
			_obligatorySyntacticFeatures = new IDBearerSet<Feature>(word._obligatorySyntacticFeatures);
			CurrentTrace = word.CurrentTrace;
		}

		public IEnumerable<Annotation<ShapeNode>> Morphs
		{
			get { return Annotations.Where(ann => ann.Type() == HCFeatureSystem.Morph); }
		}

		public IEnumerable<Allomorph> AllomorphsInMorphOrder
		{
			get { return Morphs.Select(morph => _allomorphs[(string) morph.FeatureStruct.GetValue(HCFeatureSystem.Allomorph)]); }
		}

		public RootAllomorph RootAllomorph
		{
			get { return _rootAllomorph; }

			set
			{
				CheckFrozen();
				_shape = value.Shape.DeepClone();
				SetRootAllomorph(value);
			}
		}

		private void SetRootAllomorph(RootAllomorph rootAllomorph)
		{
			_rootAllomorph = rootAllomorph;
			var entry = (LexEntry) _rootAllomorph.Morpheme;
			Stratum = entry.Stratum;
			_shape.Annotations.Add(_shape.First, _shape.Last, FeatureStruct.New()
				.Symbol(HCFeatureSystem.Morph)
				.Feature(HCFeatureSystem.Allomorph).EqualTo(_rootAllomorph.ID).Value);
			SyntacticFeatureStruct = entry.SyntacticFeatureStruct.DeepClone();
			_mprFeatures.Clear();
			_mprFeatures.UnionWith(entry.MprFeatures);
			_allomorphs.Add(_rootAllomorph);
		}

		public Shape Shape
		{
			get { return _shape; }
		}

		public FeatureStruct SyntacticFeatureStruct { get; set; }

		public FeatureStruct RealizationalFeatureStruct
		{
			get { return _realizationalFS; }
			set
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

		public IDBearerSet<Allomorph> Allomorphs
		{
			get { return _allomorphs; }
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
			set
			{
				CheckFrozen();
				_stratum = value;
			}
		}

		public Trace CurrentTrace { get; set; }

		public IEnumerable<IMorphologicalRule> MorphologicalRules
		{
			get { return _mrules; }
		}

		/// <summary>
		/// Gets the current rule.
		/// </summary>
		/// <value>The current rule.</value>
		public IMorphologicalRule CurrentMorphologicalRule
		{
			get
			{
				if (_mrules.Count == 0)
					return null;
				return _mrules.Peek();
			}
		}

		/// <summary>
		/// Notifies this analysis that the specified morphological rule was unapplied.
		/// </summary>
		/// <param name="mrule">The morphological rule.</param>
		/// <param name="realizational"> </param>
		public void MorphologicalRuleUnapplied(IMorphologicalRule mrule, bool realizational)
		{
			CheckFrozen();
			_mrulesUnapplied.UpdateValue(mrule, () => 0, count => count + 1);
			if (!realizational)
				_mrules.Push(mrule);
		}

		/// <summary>
		/// Gets the number of times the specified morphological rule has been unapplied.
		/// </summary>
		/// <param name="mrule">The morphological rule.</param>
		/// <returns>The number of unapplications.</returns>
		public int GetUnapplicationCount(IMorphologicalRule mrule)
		{
			int numUnapplies;
			if (!_mrulesUnapplied.TryGetValue(mrule, out numUnapplies))
				numUnapplies = 0;
			return numUnapplies;
		}

		/// <summary>
		/// Notifies this word synthesis that the specified morphological rule has applied.
		/// </summary>
		public void MorphologicalRuleApplied(IMorphologicalRule mrule)
		{
			CheckFrozen();
			_mrulesApplied.UpdateValue(mrule, () => 0, count => count + 1);
		}

		public void CurrentMorphologicalRuleApplied()
		{
			CheckFrozen();
			IMorphologicalRule mrule = _mrules.Pop();
			MorphologicalRuleApplied(mrule);
		}

		/// <summary>
		/// Gets the number of times the specified morphological rule has been applied.
		/// </summary>
		/// <param name="mrule">The morphological rule.</param>
		/// <returns>The number of applications.</returns>
		public int GetApplicationCount(IMorphologicalRule mrule)
		{
			int numApplies;
			if (!_mrulesApplied.TryGetValue(mrule, out numApplies))
				numApplies = 0;
			return numApplies;
		}

		public Word CurrentNonHead
		{
			get
			{
				if (_nonHeads.Count == 0)
					return null;
				return _nonHeads.Peek();
			}
		}

		public int NonHeadCount
		{
			get { return _nonHeads.Count; }
		}

		public void NonHeadUnapplied(Word nonHead)
		{
			CheckFrozen();
			_nonHeads.Push(nonHead);
		}

		public void CurrentNonHeadApplied()
		{
			CheckFrozen();
			_nonHeads.Pop();
		}

		public bool CheckBlocking(out Word word)
		{
			word = null;
			LexFamily family = ((LexEntry) RootAllomorph.Morpheme).Family;
			if (family == null)
				return false;

			foreach (LexEntry entry in family.Entries)
			{
				if (entry != RootAllomorph.Morpheme && entry.Stratum == Stratum && entry.SyntacticFeatureStruct.ValueEquals(SyntacticFeatureStruct))
				{
					word = new Word(entry.PrimaryAllomorph, RealizationalFeatureStruct.DeepClone()) { CurrentTrace = CurrentTrace };
					word.Freeze();
					return true;
				}
			}

			return false;
		}

		public void ResetDirty()
		{
			CheckFrozen();
			foreach (ShapeNode node in _shape)
				node.SetDirty(false);
		}

		protected override int FreezeImpl()
		{
			int code = 23;
			_shape.Freeze();
			code = code * 31 + _shape.GetFrozenHashCode();
			_realizationalFS.Freeze();
			code = code * 31 + _realizationalFS.GetFrozenHashCode();
			foreach (Word nonHead in _nonHeads)
			{
				nonHead.Freeze();
				code = code * 31 + nonHead.GetFrozenHashCode();
			}
			code = code * 31 + _stratum.GetHashCode();
			code = code * 31 + (_rootAllomorph == null ? 0 : _rootAllomorph.GetHashCode());
			code = code * 31 + _mrules.GetSequenceHashCode();
			return code;
		}

		public override bool ValueEquals(Word other)
		{
			if (other == null)
				return false;

			return _shape.ValueEquals(other._shape) && _realizationalFS.ValueEquals(other._realizationalFS)
				&& _nonHeads.SequenceEqual(other._nonHeads, FreezableEqualityComparer<Word>.Default) && _stratum == other._stratum
				&& _rootAllomorph == other._rootAllomorph && _mrules.SequenceEqual(other._mrules);
		}

		public Word DeepClone()
		{
			return new Word(this);
		}

		public override string ToString()
		{
			return Stratum.SymbolTable.ToRegexString(Shape, true);
		}
	}
}
