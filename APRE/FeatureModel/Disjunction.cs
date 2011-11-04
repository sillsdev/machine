using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.APRE.FeatureModel
{
	public class Disjunction : IEnumerable<FeatureStruct>, IEquatable<Disjunction>, ICloneable<Disjunction>
	{
		private readonly HashSet<FeatureStruct> _disjuncts;

		public Disjunction(IEnumerable<FeatureStruct> disjuncts)
		{
			_disjuncts = new HashSet<FeatureStruct>(disjuncts);
			if (_disjuncts.Count < 2)
				throw new ArgumentException("At least two disjuncts must be specified.", "disjuncts");
		}

		public Disjunction(Disjunction disjunction)
		{
			_disjuncts = new HashSet<FeatureStruct>(disjunction._disjuncts.Clone());
		}

		public IEnumerator<FeatureStruct> GetEnumerator()
		{
			return _disjuncts.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public bool Negation(out FeatureStruct output)
		{
			output = null;
			foreach (FeatureStruct disjunct in _disjuncts)
			{
				FeatureStruct negation;
				if (!disjunct.Negation(out negation))
				{
					output = null;
					return false;
				}

				if (output == null)
				{
					output = negation;
				}
				else
				{
					if (!output.Unify(negation, out output))
					{
						output = null;
						return false;
					}
				}
			}
			return true;
		}

		internal Disjunction Clone(IDictionary<FeatureValue, FeatureValue> copies)
		{
			return new Disjunction(_disjuncts.Select(disj => (FeatureStruct) disj.Clone(new Dictionary<FeatureValue, FeatureValue>(copies, new ReferenceEqualityComparer<FeatureValue>()))));
		}

		public Disjunction Clone()
		{
			return new Disjunction(this);
		}

		public bool Equals(Disjunction other)
		{
			return other != null && _disjuncts.SetEquals(other._disjuncts);
		}

		public override bool Equals(object obj)
		{
			var disjunction = obj as Disjunction;
			return disjunction != null && Equals(disjunction);
		}

		public override int GetHashCode()
		{
			return _disjuncts.Aggregate(23, (code, fs) => code * 31 + fs.GetHashCode());
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			bool first = true;
			foreach (FeatureStruct disjunct in _disjuncts)
			{
				if (!first)
					sb.Append(" || ");
				sb.Append(disjunct);
				first = false;
			}
			return sb.ToString();
		}
	}
}
