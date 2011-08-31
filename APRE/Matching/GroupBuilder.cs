using System;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Matching
{
	public class GroupBuilder<TOffset> : PatternNodeBuilder<TOffset>, IQuantifiableGroupBuilder<TOffset>
	{
		private readonly string _name;

		public GroupBuilder()
		{
		}

		public GroupBuilder(string name)
		{
			_name = name;
		}

		public IGroupBuilder<TOffset> Or
		{
			get
			{
				AddAlternative();
				return this;
			}
		}

		public IQuantifiableGroupBuilder<TOffset> Group(string name, Func<IGroupBuilder<TOffset>, IGroupBuilder<TOffset>> build)
		{
			AddGroup(name, build);
			return this;
		}

		public IQuantifiableGroupBuilder<TOffset> Group(Func<IGroupBuilder<TOffset>, IGroupBuilder<TOffset>> build)
		{
			AddGroup(null, build);
			return this;
		}

		public IQuantifiableGroupBuilder<TOffset> Annotation(FeatureStruct fs)
		{
			AddAnnotation(fs);
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

		IGroupBuilder<TOffset> IQuantifiableGroupBuilder<TOffset>.ZeroOrMore
		{
			get
			{
				AddQuantifier(0, Quantifier<TOffset>.Infinite);
				return this;
			}
		}

		IGroupBuilder<TOffset> IQuantifiableGroupBuilder<TOffset>.OneOrMore
		{
			get
			{
				AddQuantifier(1, Quantifier<TOffset>.Infinite);
				return this;
			}
		}

		IGroupBuilder<TOffset> IQuantifiableGroupBuilder<TOffset>.Optional
		{
			get
			{
				AddQuantifier(0, 1);
				return this;
			}
		}

		IGroupBuilder<TOffset> IQuantifiableGroupBuilder<TOffset>.Range(int min, int max)
		{
			AddQuantifier(min, max);
			return this;
		}
	}
}
