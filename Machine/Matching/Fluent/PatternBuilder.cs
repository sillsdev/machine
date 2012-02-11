using System;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Matching.Fluent
{
	public class PatternBuilder<TData, TOffset> : PatternNodeBuilder<TData, TOffset>, IPatternSyntax<TData, TOffset>, IQuantifierPatternSyntax<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly string _name;
		private Func<Match<TData, TOffset>, bool> _acceptable = match => true;

		public PatternBuilder()
		{
		}

		public PatternBuilder(string name)
		{
			_name = name;
		}

		public INodesPatternSyntax<TData, TOffset> Subpattern(Func<IPatternSyntax<TData, TOffset>, IPatternSyntax<TData, TOffset>> build)
		{
			AddSubpattern(null, build);
			return this;
		}

		public INodesPatternSyntax<TData, TOffset> Subpattern(string name, Func<IPatternSyntax<TData, TOffset>, IPatternSyntax<TData, TOffset>> build)
		{
			AddSubpattern(name, build);
			return this;
		}

		public IQuantifierPatternSyntax<TData, TOffset> Annotation(FeatureStruct fs)
		{
			AddAnnotation(fs);
			return this;
		}

		public Pattern<TData, TOffset> Value
		{
			get
			{
				var pattern = new Pattern<TData, TOffset>(_name) {Acceptable = _acceptable};
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

		public INodesPatternSyntax<TData, TOffset> MatchAcceptableWhere(Func<Match<TData, TOffset>, bool> acceptable)
		{
			_acceptable = acceptable;
			return this;
		}
	}
}
