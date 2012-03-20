using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Collections;
using SIL.Machine.Matching.Fluent;

namespace SIL.Machine.Matching
{
	public class Pattern<TData, TOffset> : PatternNode<TData, TOffset>, IDeepCloneable<Pattern<TData, TOffset>> where TData : IData<TOffset>
	{
		public static IPatternSyntax<TData, TOffset> New()
		{
			return new PatternBuilder<TData, TOffset>();
		}

		public static IPatternSyntax<TData, TOffset> New(string name)
		{
			return new PatternBuilder<TData, TOffset>(name);
		}

		private readonly string _name;

		public Pattern()
			: this(Enumerable.Empty<PatternNode<TData, TOffset>>())
		{
		}

		public Pattern(IEnumerable<PatternNode<TData, TOffset>> nodes)
			: base(nodes)
		{
			Acceptable = match => true;
		}

		public Pattern(string name)
			: this(name, Enumerable.Empty<PatternNode<TData, TOffset>>())
		{
		}

		public Pattern(string name, IEnumerable<PatternNode<TData, TOffset>> nodes)
			: base(nodes)
		{
			Acceptable = match => true;
			_name = name;
		}

		protected Pattern(Pattern<TData, TOffset> pattern)
			: base(pattern)
		{
			_name = pattern._name;
			Acceptable = pattern.Acceptable;
		}

		public string Name
		{
			get { return _name; }
		}

		public Func<Match<TData, TOffset>, bool> Acceptable { get; set; }

		protected override PatternNode<TData, TOffset> DeepCloneImpl()
		{
			return new Pattern<TData, TOffset>(this);
		}

		public new Pattern<TData, TOffset> DeepClone()
		{
			return new Pattern<TData, TOffset>(this);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach (PatternNode<TData, TOffset> node in Children)
				sb.Append(node.ToString());
			return sb.ToString();
		}
	}
}
