using System;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Matching.Fluent
{
	public class GroupBuilder<TData, TOffset> : PatternNodeBuilder<TData, TOffset>, IQuantifierGroupSyntax<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly string _name;

		public GroupBuilder()
		{
		}

		public GroupBuilder(string name)
		{
			_name = name;
		}

		IGroupSyntax<TData, TOffset> IAlternationGroupSyntax<TData, TOffset>.Or
		{
			get
			{
				AddAlternative();
				return this;
			}
		}

		public IQuantifierGroupSyntax<TData, TOffset> Group(string name, Func<IGroupSyntax<TData, TOffset>, IGroupSyntax<TData, TOffset>> build)
		{
			AddGroup(name, build);
			return this;
		}

		public IQuantifierGroupSyntax<TData, TOffset> Group(Func<IGroupSyntax<TData, TOffset>, IGroupSyntax<TData, TOffset>> build)
		{
			AddGroup(null, build);
			return this;
		}

		public IQuantifierGroupSyntax<TData, TOffset> Annotation(string type, FeatureStruct fs)
		{
			AddAnnotation(type, fs);
			return this;
		}

		public Group<TData, TOffset> Value
		{
			get
			{
				var group = new Group<TData, TOffset>(_name);
				PopulateNode(group);
				return group;
			}
		}

		IAlternationGroupSyntax<TData, TOffset> IQuantifierGroupSyntax<TData, TOffset>.ZeroOrMore
		{
			get
			{
				AddQuantifier(0, Quantifier<TData, TOffset>.Infinite, true);
				return this;
			}
		}

		IAlternationGroupSyntax<TData, TOffset> IQuantifierGroupSyntax<TData, TOffset>.LazyZeroOrMore
		{
			get
			{
				AddQuantifier(0, Quantifier<TData, TOffset>.Infinite, false);
				return this;
			}
		}

		IAlternationGroupSyntax<TData, TOffset> IQuantifierGroupSyntax<TData, TOffset>.OneOrMore
		{
			get
			{
				AddQuantifier(1, Quantifier<TData, TOffset>.Infinite, true);
				return this;
			}
		}

		IAlternationGroupSyntax<TData, TOffset> IQuantifierGroupSyntax<TData, TOffset>.LazyOneOrMore
		{
			get
			{
				AddQuantifier(1, Quantifier<TData, TOffset>.Infinite, false);
				return this;
			}
		}

		IAlternationGroupSyntax<TData, TOffset> IQuantifierGroupSyntax<TData, TOffset>.Optional
		{
			get
			{
				AddQuantifier(0, 1, true);
				return this;
			}
		}

		IAlternationGroupSyntax<TData, TOffset> IQuantifierGroupSyntax<TData, TOffset>.LazyOptional
		{
			get
			{
				AddQuantifier(0, 1, false);
				return this;
			}
		}

		IAlternationGroupSyntax<TData, TOffset> IQuantifierGroupSyntax<TData, TOffset>.Range(int min, int max)
		{
			AddQuantifier(min, max, true);
			return this;
		}

		IAlternationGroupSyntax<TData, TOffset> IQuantifierGroupSyntax<TData, TOffset>.LazyRange(int min, int max)
		{
			AddQuantifier(min, max, false);
			return this;
		}
	}
}
