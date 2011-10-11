using System;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Matching.Fluent
{
	public class PatternBuilder<TOffset> : PatternNodeBuilder<TOffset>, IPatternSyntax<TOffset>, IQuantifierPatternSyntax<TOffset>
	{
		private readonly SpanFactory<TOffset> _spanFactory;
		private Func<Annotation<TOffset>, bool> _filter;
		private Direction _dir;

		public PatternBuilder(SpanFactory<TOffset> spanFactory)
		{
			_spanFactory = spanFactory;
			_dir = Direction.LeftToRight;
			_filter = ann => true;
		}

		public IPatternSyntax<TOffset> MatchLeftToRight
		{
			get
			{
				_dir = Direction.LeftToRight;
				return this;
			}
		}

		public IPatternSyntax<TOffset> MatchRightToLeft
		{
			get
			{
				_dir = Direction.RightToLeft;
				return this;
			}
		}

		public IPatternSyntax<TOffset> AnnotationsAllowableWhere(Func<Annotation<TOffset>, bool> filter)
		{
			_filter = filter;
			return this;
		}

		public INodesPatternSyntax<TOffset> Expression(Func<IExpressionSyntax<TOffset>, IExpressionSyntax<TOffset>> build)
		{
			AddExpression(null, build);
			return this;
		}

		public INodesPatternSyntax<TOffset> Expression(string name, Func<IExpressionSyntax<TOffset>, IExpressionSyntax<TOffset>> build)
		{
			AddExpression(name, build);
			return this;
		}

		public Pattern<TOffset> Value
		{
			get
			{
				var pattern = new Pattern<TOffset>(_spanFactory, _dir, _filter);
				PopulateNode(pattern);
				return pattern;
			}
		}

		public IQuantifierPatternSyntax<TOffset> Group(string name, Func<IGroupSyntax<TOffset>, IGroupSyntax<TOffset>> build)
		{
			AddGroup(name, build);
			return this;
		}

		public IQuantifierPatternSyntax<TOffset> Group(Func<IGroupSyntax<TOffset>, IGroupSyntax<TOffset>> build)
		{
			AddGroup(null, build);
			return this;
		}

		public IQuantifierPatternSyntax<TOffset> Annotation(string type, FeatureStruct fs)
		{
			AddAnnotation(type, fs);
			return this;
		}

		IInitialNodesPatternSyntax<TOffset> IAlternationPatternSyntax<TOffset>.Or
		{
			get
			{
				AddAlternative();
				return this;
			}
		}

		IAlternationPatternSyntax<TOffset> IQuantifierPatternSyntax<TOffset>.ZeroOrMore
		{
			get
			{
				AddQuantifier(0, Quantifier<TOffset>.Infinite, true);
				return this;
			}
		}

		IAlternationPatternSyntax<TOffset> IQuantifierPatternSyntax<TOffset>.LazyZeroOrMore
		{
			get
			{
				AddQuantifier(0, Quantifier<TOffset>.Infinite, false);
				return this;
			}
		}

		IAlternationPatternSyntax<TOffset> IQuantifierPatternSyntax<TOffset>.OneOrMore
		{
			get
			{
				AddQuantifier(1, Quantifier<TOffset>.Infinite, true);
				return this;
			}
		}

		IAlternationPatternSyntax<TOffset> IQuantifierPatternSyntax<TOffset>.LazyOneOrMore
		{
			get
			{
				AddQuantifier(1, Quantifier<TOffset>.Infinite, false);
				return this;
			}
		}

		IAlternationPatternSyntax<TOffset> IQuantifierPatternSyntax<TOffset>.Optional
		{
			get
			{
				AddQuantifier(0, 1, true);
				return this;
			}
		}

		IAlternationPatternSyntax<TOffset> IQuantifierPatternSyntax<TOffset>.LazyOptional
		{
			get
			{
				AddQuantifier(0, 1, false);
				return this;
			}
		}

		IAlternationPatternSyntax<TOffset> IQuantifierPatternSyntax<TOffset>.Range(int min, int max)
		{
			AddQuantifier(min, max, true);
			return this;
		}

		IAlternationPatternSyntax<TOffset> IQuantifierPatternSyntax<TOffset>.LazyRange(int min, int max)
		{
			AddQuantifier(min, max, false);
			return this;
		}
	}
}
