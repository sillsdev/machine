using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.Machine.Annotations;
using SIL.Machine.Matching.Fluent;
using SIL.ObjectModel;

namespace SIL.Machine.Matching
{
	public class Pattern<TData, TOffset> : PatternNode<TData, TOffset>, ICloneable<Pattern<TData, TOffset>>, IValueEquatable<Pattern<TData, TOffset>> where TData : IAnnotatedData<TOffset>
	{
		public static IPatternSyntax<TData, TOffset> New()
		{
			return new PatternBuilder<TData, TOffset>();
		}

		public static IPatternSyntax<TData, TOffset> NewMutable()
		{
			return new PatternBuilder<TData, TOffset>(true);
		}

		public static IPatternSyntax<TData, TOffset> New(string name)
		{
			return new PatternBuilder<TData, TOffset>(name);
		}

		public static IPatternSyntax<TData, TOffset> NewMutable(string name)
		{
			return new PatternBuilder<TData, TOffset>(name, true);
		}

		private readonly string _name;
		private Func<Match<TData, TOffset>, bool> _acceptable;

		public Pattern()
			: this(Enumerable.Empty<PatternNode<TData, TOffset>>())
		{
		}

		public Pattern(IEnumerable<PatternNode<TData, TOffset>> nodes)
			: base(nodes)
		{
		}

		public Pattern(params PatternNode<TData, TOffset>[] nodes)
			: base(nodes)
		{
		}

		public Pattern(string name)
			: this(name, Enumerable.Empty<PatternNode<TData, TOffset>>())
		{
		}

		public Pattern(string name, IEnumerable<PatternNode<TData, TOffset>> nodes)
			: base(nodes)
		{
			_name = name;
		}

		public Pattern(string name, params PatternNode<TData, TOffset>[] nodes)
			: base(nodes)
		{
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

		public Func<Match<TData, TOffset>, bool> Acceptable
		{
			get { return _acceptable; }
			set
			{
				CheckFrozen();
				_acceptable = value;
			}
		}

		protected override PatternNode<TData, TOffset> CloneImpl()
		{
			return new Pattern<TData, TOffset>(this);
		}

		public new Pattern<TData, TOffset> Clone()
		{
			return new Pattern<TData, TOffset>(this);
		}

		public override bool ValueEquals(PatternNode<TData, TOffset> other)
		{
			var otherPattern = other as Pattern<TData, TOffset>;
			return otherPattern != null && ValueEquals(otherPattern);
		}

		protected override int FreezeImpl()
		{
			int code = base.FreezeImpl();
			if (Acceptable != null)
				code = code * 31 + Acceptable.GetHashCode();
			return code;
		}

		public bool ValueEquals(Pattern<TData, TOffset> other)
		{
			if (this == other)
				return true;

			if (other == null)
				return false;

			return Acceptable == other.Acceptable && base.ValueEquals(other);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach (PatternNode<TData, TOffset> node in Children)
				sb.Append(node);
			return sb.ToString();
		}
	}
}
