using System.Collections.Generic;
using System.Linq;
using SIL.Machine;
using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This enumeration represents the morpher mode type.
	/// </summary>
	public enum Mode
	{
		/// <summary>
		/// Analysis mode (unapplication of rules)
		/// </summary>
		Analysis,
		/// <summary>
		/// Synthesis mode (application of rules)
		/// </summary>
		Synthesis
	}

	public class Word : IData<ShapeNode>, ICloneable<Word>
	{
		private readonly Shape _shape;
		private readonly Stack<MorphologicalRule> _mrules;
		private readonly Dictionary<MorphologicalRule, int> _mrulesUnapplied;
		private readonly Dictionary<MorphologicalRule, int> _mrulesApplied;
		private readonly IDBearerSet<Allomorph> _allomorphs; 

		public Word(Stratum stratum, string word)
		{
			Stratum = stratum;
			if (!stratum.SymbolDefinitionTable.ToShape(word, out _shape))
			{
				var me = new MorphException(MorphErrorCode.InvalidShape,
					string.Format(HCStrings.kstidInvalidWord, word, stratum.SymbolDefinitionTable.ID));
				me.Data["shape"] = word;
				me.Data["charDefTable"] = stratum.SymbolDefinitionTable.ID;
			}
			SyntacticFeatureStruct = new FeatureStruct();
			_mrules = new Stack<MorphologicalRule>();
			_mrulesUnapplied = new Dictionary<MorphologicalRule, int>();
			_mrulesApplied = new Dictionary<MorphologicalRule, int>();
			_allomorphs = new IDBearerSet<Allomorph>();
		}

		public Word(Word word)
		{
			Stratum = word.Stratum;
			_shape = new Shape(word._shape.SpanFactory, word._shape.Begin.Clone(), word._shape.End.Clone());
			_allomorphs = new IDBearerSet<Allomorph>();
			word.CopyTo(word._shape.First, word._shape.Last, this);
			SyntacticFeatureStruct = word.SyntacticFeatureStruct.Clone();
			_mrules = new Stack<MorphologicalRule>(word._mrules);
			Root = word.Root;
			_mrulesUnapplied = new Dictionary<MorphologicalRule, int>(word._mrulesUnapplied);
			_mrulesApplied = new Dictionary<MorphologicalRule, int>(word._mrulesApplied);
		}

		public Stratum Stratum { get; set; }

		public Trace CurrentTrace { get; set; }

		public LexEntry Root { get; internal set; }

		public Shape Shape
		{
			get { return _shape; }
		}

		public FeatureStruct SyntacticFeatureStruct { get; set; }

		public Span<ShapeNode> Span
		{
			get { return _shape.Span; }
		}

		public AnnotationList<ShapeNode> Annotations
		{
			get { return _shape.Annotations; }
		}

		public IEnumerable<Morph> Morphs
		{
			get
			{
				return from morphAnn in _shape.Annotations.GetNodes(HCFeatureSystem.MorphType)
					   select new Morph(morphAnn.Span, _allomorphs[(string) morphAnn.FeatureStruct.GetValue(HCFeatureSystem.Allomorph)]);
			}
		}

		public Span<ShapeNode> CopyTo(ShapeNode srcStart, ShapeNode srcEnd, Word dest)
		{
			return CopyTo(_shape.SpanFactory.Create(srcStart, srcEnd), dest);
		}

		public Span<ShapeNode> CopyTo(Span<ShapeNode> srcSpan, Word dest)
		{
			Span<ShapeNode> destSpan = _shape.CopyTo(srcSpan, dest._shape);
			Dictionary<ShapeNode, ShapeNode> mapping = _shape.GetNodes(srcSpan).Zip(dest._shape.GetNodes(destSpan)).ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
			foreach (Annotation<ShapeNode> morphAnn in _shape.Annotations.GetNodes(srcSpan).Where(ann => ann.Type == HCFeatureSystem.MorphType))
			{
				var id = (string) morphAnn.FeatureStruct.GetValue(HCFeatureSystem.Allomorph);
				dest.MarkMorph(mapping[morphAnn.Span.Start], mapping[morphAnn.Span.End], _allomorphs[id]);
			}

			return destSpan;
		}

		internal void MarkMorph(ShapeNode start, ShapeNode end, Allomorph allomorph)
		{
			MarkMorph(_shape.SpanFactory.Create(start, end), allomorph);
		}

		internal void MarkMorph(Span<ShapeNode> span, Allomorph allomorph)
		{
			_shape.Annotations.Add(HCFeatureSystem.MorphType, span,
				FeatureStruct.New().Feature(HCFeatureSystem.Allomorph).EqualTo(allomorph.ID).Value);
			_allomorphs.Add(allomorph);
		}

		/// <summary>
		/// Gets the current rule.
		/// </summary>
		/// <value>The current rule.</value>
		internal MorphologicalRule CurrentRule
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
		internal void MorphologicalRuleUnapplied(MorphologicalRule mrule)
		{
			int numUnapplies = GetNumUnappliesForMorphologicalRule(mrule);
			_mrulesUnapplied[mrule] = numUnapplies + 1;
			_mrules.Push(mrule);
		}

		/// <summary>
		/// Gets the number of times the specified morphological rule has been unapplied.
		/// </summary>
		/// <param name="mrule">The morphological rule.</param>
		/// <returns>The number of unapplications.</returns>
		internal int GetNumUnappliesForMorphologicalRule(MorphologicalRule mrule)
		{
			int numUnapplies;
			if (!_mrulesUnapplied.TryGetValue(mrule, out numUnapplies))
				numUnapplies = 0;
			return numUnapplies;
		}

		/// <summary>
		/// Notifies this word synthesis that the specified morphological rule has applied.
		/// </summary>
		/// <param name="mrule">The morphological rule.</param>
		internal void MorphologicalRuleApplied(MorphologicalRule mrule)
		{
			int numApplies = GetNumAppliesForMorphologicalRule(mrule);
			_mrulesApplied[mrule] = numApplies + 1;
			if (mrule == CurrentRule)
				_mrules.Pop();
		}

		/// <summary>
		/// Gets the number of times the specified morphological rule has been applied.
		/// </summary>
		/// <param name="mrule">The morphological rule.</param>
		/// <returns>The number of applications.</returns>
		internal int GetNumAppliesForMorphologicalRule(MorphologicalRule mrule)
		{
			int numApplies;
			if (!_mrulesApplied.TryGetValue(mrule, out numApplies))
				numApplies = 0;
			return numApplies;
		}

		public Word Clone()
		{
			return new Word(this);
		}

		public override string ToString()
		{
			return Stratum.SymbolDefinitionTable.ToRegexString(Shape, true);
		}
	}
}
