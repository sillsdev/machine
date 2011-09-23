using System;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Matching.Fluent
{
	public class GroupBuilder<TOffset> : PatternNodeBuilder<TOffset>, IQuantifierGroupSyntax<TOffset>
	{
		private readonly string _name;

		public GroupBuilder()
		{
		}

		public GroupBuilder(string name)
		{
			_name = name;
		}

		IGroupSyntax<TOffset> IAlternationGroupSyntax<TOffset>.Or
		{
			get
			{
				AddAlternative();
				return this;
			}
		}

		public IQuantifierGroupSyntax<TOffset> Group(string name, Func<IGroupSyntax<TOffset>, IGroupSyntax<TOffset>> build)
		{
			AddGroup(name, build);
			return this;
		}

		public IQuantifierGroupSyntax<TOffset> Group(Func<IGroupSyntax<TOffset>, IGroupSyntax<TOffset>> build)
		{
			AddGroup(null, build);
			return this;
		}

		public IQuantifierGroupSyntax<TOffset> Annotation(string type, FeatureStruct fs)
		{
			AddAnnotation(type, fs);
			return this;
		}

		public Group<TOffset> Value
		{
			get
			{
				var group = new Group<TOffset>(_name);
				PopulateNode(group);
				return group;
			}
		}

		IAlternationGroupSyntax<TOffset> IQuantifierGroupSyntax<TOffset>.ZeroOrMore
		{
			get
			{
				AddQuantifier(0, Quantifier<TOffset>.Infinite, true);
				return this;
			}
		}

		IAlternationGroupSyntax<TOffset> IQuantifierGroupSyntax<TOffset>.LazyZeroOrMore
		{
			get
			{
				AddQuantifier(0, Quantifier<TOffset>.Infinite, false);
				return this;
			}
		}

		IAlternationGroupSyntax<TOffset> IQuantifierGroupSyntax<TOffset>.OneOrMore
		{
			get
			{
				AddQuantifier(1, Quantifier<TOffset>.Infinite, true);
				return this;
			}
		}

		IAlternationGroupSyntax<TOffset> IQuantifierGroupSyntax<TOffset>.LazyOneOrMore
		{
			get
			{
				AddQuantifier(1, Quantifier<TOffset>.Infinite, false);
				return this;
			}
		}

		IAlternationGroupSyntax<TOffset> IQuantifierGroupSyntax<TOffset>.Optional
		{
			get
			{
				AddQuantifier(0, 1, true);
				return this;
			}
		}

		IAlternationGroupSyntax<TOffset> IQuantifierGroupSyntax<TOffset>.LazyOptional
		{
			get
			{
				AddQuantifier(0, 1, false);
				return this;
			}
		}

		IAlternationGroupSyntax<TOffset> IQuantifierGroupSyntax<TOffset>.Range(int min, int max)
		{
			AddQuantifier(min, max, true);
			return this;
		}

		IAlternationGroupSyntax<TOffset> IQuantifierGroupSyntax<TOffset>.LazyRange(int min, int max)
		{
			AddQuantifier(min, max, false);
			return this;
		}
	}
}
