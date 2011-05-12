using SIL.APRE;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a phonological environment.
	/// </summary>
	public class Environment
	{
		private readonly Pattern<PhoneticShapeNode> _leftEnv;
		private readonly Pattern<PhoneticShapeNode> _rightEnv;

		/// <summary>
		/// Initializes a new instance of the <see cref="Environment"/> class.
		/// </summary>
		public Environment()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Environment"/> class.
		/// </summary>
		/// <param name="leftEnv">The left environment.</param>
		/// <param name="rightEnv">The right environment.</param>
		public Environment(Pattern<PhoneticShapeNode> leftEnv, Pattern<PhoneticShapeNode> rightEnv)
		{
			_leftEnv = leftEnv;
			_rightEnv = rightEnv;
		}

		/// <summary>
		/// Gets the left environment.
		/// </summary>
		/// <value>The left environment.</value>
		public Pattern<PhoneticShapeNode> LeftEnvironment
		{
			get
			{
				return _leftEnv;
			}
		}

		/// <summary>
		/// Gets the right environment.
		/// </summary>
		/// <value>The right environment.</value>
		public Pattern<PhoneticShapeNode> RightEnvironment
		{
			get
			{
				return _rightEnv;
			}
		}

		/// <summary>
		/// Checks if the specified phonetic shape matches this environment.
		/// </summary>
		/// <param name="shape">The phonetic shape.</param>
		/// <param name="leftNode">The left atom.</param>
		/// <param name="rightNode">The right atom.</param>
		/// <param name="mode">The mode.</param>
		/// <returns>
		/// 	<c>true</c> if the specified left node is match; otherwise, <c>false</c>.
		/// </returns>
		public bool IsMatch(PhoneticShape shape, PhoneticShapeNode leftNode, PhoneticShapeNode rightNode, ModeType mode)
		{
			return IsMatch(shape, leftNode, rightNode, mode, null);
		}

		/// <summary>
		/// Checks if the specified phonetic shape matches this environment.
		/// </summary>
		/// <param name="shape">The phonetic shape.</param>
		/// <param name="leftNode">The left atom.</param>
		/// <param name="rightNode">The right atom.</param>
		/// <param name="mode">The mode.</param>
		/// <param name="varValues">The instantiated variables.</param>
		/// <returns>
		/// 	<c>true</c> if the shape successfully matched this pattern, otherwise <c>false</c>.
		/// </returns>
		public bool IsMatch(PhoneticShape shape, PhoneticShapeNode leftNode, PhoneticShapeNode rightNode, ModeType mode,
			FeatureStructure varValues)
		{
			var temp = (FeatureStructure) varValues.Clone();
			// right environment
			if (_rightEnv != null)
			{
				PatternMatch<PhoneticShapeNode> match;
				if (_rightEnv.IsMatch(shape.Annotations.GetView(rightNode.Annotation), Direction.LeftToRight, mode, temp, out match))
					temp.Instantiate(match.VariableValues);
				else
					return false;
			}

			// left environment
			if (_leftEnv != null)
			{
				PatternMatch<PhoneticShapeNode> match;
				if (_leftEnv.IsMatch(shape.Annotations.GetView(leftNode.Annotation, Direction.RightToLeft), Direction.RightToLeft, mode, temp, out match))
					temp.Instantiate(match.VariableValues);
				else
					return false;
			}

			varValues.Instantiate(temp);
			return true;
		}
	}
}