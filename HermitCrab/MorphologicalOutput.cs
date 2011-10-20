using System.Collections.Generic;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This abstract class is extended by each type of morphological output record
	/// used on the RHS of morphological rules.
	/// </summary>
	public abstract class MorphologicalOutput
	{
		public bool Reduplication { get; internal set; }

		public abstract void GenerateAnalysisLhs(Pattern<Word, ShapeNode> analysisLhs, IList<Expression<Word, ShapeNode>> lhs);

		/// <summary>
		/// Applies this output record to the specified word synthesis.
		/// </summary>
		/// <param name="match">The match.</param>
		/// <param name="input"></param>
		/// <param name="output">The output word synthesis.</param>
		/// <param name="allomorph"></param>
		public abstract void Apply(PatternMatch<ShapeNode> match, Word input, Word output, Allomorph allomorph);

		protected void AddMorphAnnotation(Word output, ShapeNode startNode, ShapeNode endNode, Allomorph allomorph)
		{
			if (allomorph != null)
			{
				output.Annotations.Add(new Annotation<ShapeNode>(HCFeatureSystem.MorphType,
					output.Shape.SpanFactory.Create(startNode, output.Shape.Last), new FeatureStruct()));
			}
		}
	}
}
