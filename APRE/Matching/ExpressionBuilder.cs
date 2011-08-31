using System;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Matching
{
	public class ExpressionBuilder<TOffset> : PatternNodeBuilder<TOffset>, IQuantifiableExpressionBuilder<TOffset>
	{
		private readonly string _name;

		public ExpressionBuilder()
		{
		}

		public ExpressionBuilder(string name)
		{
			_name = name;
		}

		public IExpressionBuilder<TOffset> Or
		{
			get
			{
				AddAlternative();
				return this;
			}
		}

		public IQuantifiableExpressionBuilder<TOffset> Group(string name, Func<IGroupBuilder<TOffset>, IGroupBuilder<TOffset>> build)
		{
			AddGroup(name, build);
			return this;
		}

		public IQuantifiableExpressionBuilder<TOffset> Group(Func<IGroupBuilder<TOffset>, IGroupBuilder<TOffset>> build)
		{
			AddGroup(null, build);
			return this;
		}

		public IQuantifiableExpressionBuilder<TOffset> Annotation(FeatureStruct fs)
		{
			AddAnnotation(fs);
			return this;
		}

		public Expression<TOffset> Value
		{
			get
			{
				var expr = new Expression<TOffset>(_name);
				PopulateNode(expr);
				return expr;
			}
		}

		public IExpressionBuilder<TOffset> ZeroOrMore
		{
			get
			{
				AddQuantifier(0, Quantifier<TOffset>.Infinite);
				return this;
			}
		}

		public IExpressionBuilder<TOffset> OneOrMore
		{
			get
			{
				AddQuantifier(1, Quantifier<TOffset>.Infinite);
				return this;
			}
		}

		public IExpressionBuilder<TOffset> Optional
		{
			get
			{
				AddQuantifier(0, 1);
				return this;
			}
		}

		public IExpressionBuilder<TOffset> Range(int min, int max)
		{
			AddQuantifier(min, max);
			return this;
		}
	}
}
