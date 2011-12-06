using System;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Matching.Fluent
{
	public class PatternBuilder<TData, TOffset> : PatternNodeBuilder<TData, TOffset>, IPatternSyntax<TData, TOffset>, IQuantifierPatternSyntax<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly SpanFactory<TOffset> _spanFactory;
		private Func<Annotation<TOffset>, bool> _filter = ann => true;
		private Direction _dir = Direction.LeftToRight;

		public PatternBuilder(SpanFactory<TOffset> spanFactory)
		{
			_spanFactory = spanFactory;
		}

		public IPatternSyntax<TData, TOffset> MatchLeftToRight
		{
			get
			{
				_dir = Direction.LeftToRight;
				return this;
			}
		}

		public IPatternSyntax<TData, TOffset> MatchRightToLeft
		{
			get
			{
				_dir = Direction.RightToLeft;
				return this;
			}
		}

		public IPatternSyntax<TData, TOffset> AnnotationsAllowableWhere(Func<Annotation<TOffset>, bool> filter)
		{
			_filter = filter;
			return this;
		}

		public INodesPatternSyntax<TData, TOffset> Expression(Func<IExpressionSyntax<TData, TOffset>, IExpressionSyntax<TData, TOffset>> build)
		{
			AddExpression(null, build);
			return this;
		}

		public INodesPatternSyntax<TData, TOffset> Expression(string name, Func<IExpressionSyntax<TData, TOffset>, IExpressionSyntax<TData, TOffset>> build)
		{
			AddExpression(name, build);
			return this;
		}

		public Pattern<TData, TOffset> Value
		{
			get
			{
				var pattern = new Pattern<TData, TOffset>(_spanFactory) {Direction = _dir, Filter = _filter};
				PopulateNode(pattern);
				return pattern;
			}
		}

		public IQuantifierPatternSyntax<TData, TOffset> Group(string name, Func<IGroupSyntax<TData, TOffset>, IGroupSyntax<TData, TOffset>> build)
		{
			AddGroup(name, build);
			return this;
		}

		public IQuantifierPatternSyntax<TData, TOffset> Group(Func<IGroupSyntax<TData, TOffset>, IGroupSyntax<TData, TOffset>> build)
		{
			AddGroup(null, build);
			return this;
		}

		public IQuantifierPatternSyntax<TData, TOffset> Annotation(string type, FeatureStruct fs)
		{
			AddAnnotation(type, fs);
			return this;
		}

		IInitialNodesPatternSyntax<TData, TOffset> IAlternationPatternSyntax<TData, TOffset>.Or
		{
			get
			{
				AddAlternative();
				return this;
			}
		}

		IAlternationPatternSyntax<TData, TOffset> IQuantifierPatternSyntax<TData, TOffset>.ZeroOrMore
		{
			get
			{
				AddQuantifier(0, Quantifier<TData, TOffset>.Infinite, true);
				return this;
			}
		}

		IAlternationPatternSyntax<TData, TOffset> IQuantifierPatternSyntax<TData, TOffset>.LazyZeroOrMore
		{
			get
			{
				AddQuantifier(0, Quantifier<TData, TOffset>.Infinite, false);
				return this;
			}
		}

		IAlternationPatternSyntax<TData, TOffset> IQuantifierPatternSyntax<TData, TOffset>.OneOrMore
		{
			get
			{
				AddQuantifier(1, Quantifier<TData, TOffset>.Infinite, true);
				return this;
			}
		}

		IAlternationPatternSyntax<TData, TOffset> IQuantifierPatternSyntax<TData, TOffset>.LazyOneOrMore
		{
			get
			{
				AddQuantifier(1, Quantifier<TData, TOffset>.Infinite, false);
				return this;
			}
		}

		IAlternationPatternSyntax<TData, TOffset> IQuantifierPatternSyntax<TData, TOffset>.Optional
		{
			get
			{
				AddQuantifier(0, 1, true);
				return this;
			}
		}

		IAlternationPatternSyntax<TData, TOffset> IQuantifierPatternSyntax<TData, TOffset>.LazyOptional
		{
			get
			{
				AddQuantifier(0, 1, false);
				return this;
			}
		}

		IAlternationPatternSyntax<TData, TOffset> IQuantifierPatternSyntax<TData, TOffset>.Range(int min, int max)
		{
			AddQuantifier(min, max, true);
			return this;
		}

		IAlternationPatternSyntax<TData, TOffset> IQuantifierPatternSyntax<TData, TOffset>.LazyRange(int min, int max)
		{
			AddQuantifier(min, max, false);
			return this;
		}
	}
}
