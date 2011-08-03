using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.APRE.Fsa;

namespace SIL.APRE
{
	public class AnnotationArcCondition<TOffset> : IArcCondition<TOffset>
	{
		private readonly HashSet<Tuple<string, FeatureStructure, bool>> _conditions; 

		public AnnotationArcCondition(string annotationType, FeatureStructure fs)
		{
			_conditions = new HashSet<Tuple<string, FeatureStructure, bool>> {Tuple.Create(annotationType, fs, false)};
		}

		public AnnotationArcCondition(IEnumerable<Tuple<string, FeatureStructure, bool>> conditions)
		{
			_conditions = new HashSet<Tuple<string, FeatureStructure, bool>>(conditions);
		}

		public bool IsMatch(Annotation<TOffset> ann, ModeType mode)
		{
			if (ann == null)
				return false;

			foreach (Tuple<string, FeatureStructure, bool> condition in _conditions)
			{
				if (condition.Item3)
				{
					if (condition.Item1 == ann.Type)
						return false;
					if (condition.Item2.IsUnifiable(ann.FeatureStructure))
						return false;
				}
				else
				{
					if (condition.Item1 != ann.Type)
						return false;
					if (!condition.Item2.IsUnifiable(ann.FeatureStructure))
						return false;
				}
			}
			return true;
		}

		public IArcCondition<TOffset> Negation()
		{
			return new AnnotationArcCondition<TOffset>(_conditions.Select(cond => Tuple.Create(cond.Item1, cond.Item2, !cond.Item3)));
		}

		public IArcCondition<TOffset> Conjunction(IArcCondition<TOffset> cond)
		{
			var annCond = (AnnotationArcCondition<TOffset>) cond;

			return new AnnotationArcCondition<TOffset>(_conditions.Union(annCond._conditions));
		}

		public bool IsSatisfiable
		{
			get { return true; }
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			bool first = true;
			foreach (Tuple<string, FeatureStructure, bool> condition in _conditions)
			{
				if (!first)
					sb.Append(" && ");
				if (condition.Item3)
					sb.Append("!");
				sb.Append("(");
				sb.Append(condition.Item1);
				sb.Append(":");
				sb.Append(condition.Item2);
				sb.Append(")");
				first = false;
			}
			return sb.ToString();
		}
	}
}
