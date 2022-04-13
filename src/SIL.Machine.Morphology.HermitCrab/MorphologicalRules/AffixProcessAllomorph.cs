using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
	public enum ReduplicationHint
	{
		/// <summary>
		/// Implicit
		/// </summary>
		Implicit,
		/// <summary>
		/// Prefix
		/// </summary>
		Prefix,
		/// <summary>
		/// Suffix
		/// </summary>
		Suffix
	}

	public class AffixProcessAllomorph : Allomorph
	{
		private readonly List<Pattern<Word, ShapeNode>> _lhs;
		private readonly List<MorphologicalOutputAction> _rhs;
		private readonly MprFeatureSet _requiredMprFeatures;
		private readonly MprFeatureSet _excludedMprFeatures;
		private readonly MprFeatureSet _outMprFeatures;

		public AffixProcessAllomorph()
		{
			_lhs = new List<Pattern<Word, ShapeNode>>();
			_rhs = new List<MorphologicalOutputAction>();
			_requiredMprFeatures = new MprFeatureSet();
			_excludedMprFeatures = new MprFeatureSet();
			_outMprFeatures = new MprFeatureSet();
			RequiredSyntacticFeatureStruct = FeatureStruct.New().Value;
		}

		public ReduplicationHint ReduplicationHint { get; set; }

		public IList<Pattern<Word, ShapeNode>> Lhs
		{
			get { return _lhs; }
		}

		public IList<MorphologicalOutputAction> Rhs
		{
			get { return _rhs; }
		}

		public MprFeatureSet RequiredMprFeatures
		{
			get { return _requiredMprFeatures; }
		}

		public MprFeatureSet ExcludedMprFeatures
		{
			get { return _excludedMprFeatures; }
		}

		public MprFeatureSet OutMprFeatures
		{
			get { return _outMprFeatures; }
		}

		public FeatureStruct RequiredSyntacticFeatureStruct { get; set; }

		protected override bool ConstraintsEqual(Allomorph other)
		{
			if (!(other is AffixProcessAllomorph otherAllo))
				return false;

			return base.ConstraintsEqual(other) && _requiredMprFeatures.SetEquals(otherAllo._requiredMprFeatures)
				&& _excludedMprFeatures.SetEquals(otherAllo._excludedMprFeatures)
				&& _lhs.SequenceEqual(otherAllo._lhs, FreezableEqualityComparer<Pattern<Word, ShapeNode>>.Default)
				&& RequiredSyntacticFeatureStruct.ValueEquals(otherAllo.RequiredSyntacticFeatureStruct);
		}

		protected override bool CheckAllomorphConstraints(Morpher morpher, Allomorph allomorph, Word word)
		{
			if (!RequiredSyntacticFeatureStruct.IsUnifiable(word.SyntacticFeatureStruct))
			{
				if (morpher != null && morpher.TraceManager.IsTracing)
				{
					morpher.TraceManager.Failed(morpher.Language, word, FailureReason.RequiredSyntacticFeatureStruct,
						this, RequiredSyntacticFeatureStruct);
				}
				return false;
			}

			return base.CheckAllomorphConstraints(morpher, allomorph, word);
		}
	}
}
