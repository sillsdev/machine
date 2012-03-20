using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.Matching;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a phonological environment.
	/// </summary>
	public class AllomorphEnvironment
	{
		private readonly Matcher<Word, ShapeNode> _leftEnvMatcher;
		private readonly Matcher<Word, ShapeNode> _rightEnvMatcher;

		/// <summary>
		/// Initializes a new instance of the <see cref="AllomorphEnvironment"/> class.
		/// </summary>
		/// <param name="spanFactory"> </param>
		/// <param name="leftEnv">The left environment.</param>
		/// <param name="rightEnv">The right environment.</param>
		public AllomorphEnvironment(SpanFactory<ShapeNode> spanFactory, Pattern<Word, ShapeNode> leftEnv, Pattern<Word, ShapeNode> rightEnv)
		{
			if (leftEnv != null && !leftEnv.IsLeaf)
			{
				_leftEnvMatcher = new Matcher<Word, ShapeNode>(spanFactory, leftEnv,
					new MatcherSettings<ShapeNode>
						{
							AnchoredToStart = true,
							Direction = Direction.RightToLeft,
							UseDefaults = true,
							Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Boundary, HCFeatureSystem.Anchor)
						});
			}
			if (rightEnv != null && !rightEnv.IsLeaf)
			{
				_rightEnvMatcher = new Matcher<Word, ShapeNode>(spanFactory, rightEnv,
					new MatcherSettings<ShapeNode>
						{
							AnchoredToStart = true,
							UseDefaults = true,
							Filter = ann => ann.Type().IsOneOf(HCFeatureSystem.Segment, HCFeatureSystem.Boundary, HCFeatureSystem.Anchor)
						});
			}
		}

		public Allomorph Allomorph { get; internal set; }

		public bool IsMatch(Word word)
		{
			foreach (Annotation<ShapeNode> morph in word.Morphs.Where(ann => ((string) ann.FeatureStruct.GetValue(HCFeatureSystem.Allomorph)) == Allomorph.ID))
			{
				if (_leftEnvMatcher != null && !_leftEnvMatcher.IsMatch(word, morph.Span.Start.Prev))
					return false;

				if (_rightEnvMatcher != null && !_rightEnvMatcher.IsMatch(word, morph.Span.End.Next))
					return false;
			}
			return true;
		}
	}
}