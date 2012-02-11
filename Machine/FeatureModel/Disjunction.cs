using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.Machine.FeatureModel
{
	internal class Disjunction : IReadOnlyCollection<FeatureStruct>
	{
		private readonly List<FeatureStruct> _disjuncts;

		public Disjunction(IEnumerable<FeatureStruct> disjuncts)
		{
			_disjuncts = new List<FeatureStruct>(disjuncts);
		}

		public Disjunction(Disjunction disjunction)
		{
			_disjuncts = new List<FeatureStruct>(disjunction._disjuncts.Clone());
		}

		IEnumerator<FeatureStruct> IEnumerable<FeatureStruct>.GetEnumerator()
		{
			return _disjuncts.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _disjuncts.GetEnumerator();
		}

		internal bool HasVariables
		{
			get { return _disjuncts.Any(disjunct => disjunct.HasVariables); }
		}

		public int Count
		{
			get { return _disjuncts.Count; }
		}

		internal bool Negation(out FeatureStruct output)
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

		internal void ReplaceVariables(VariableBindings varBindings)
		{
			foreach (FeatureStruct disjunct in _disjuncts)
				disjunct.ReplaceVariables(varBindings);
		}

		internal Disjunction Clone(IDictionary<FeatureValue, FeatureValue> copies)
		{
			return new Disjunction(_disjuncts.Select(disj => (FeatureStruct) disj.Clone(new Dictionary<FeatureValue, FeatureValue>(copies, new ReferenceEqualityComparer<FeatureValue>()))));
		}

		internal void FindReentrances(IDictionary<FeatureValue, bool> reentrances)
		{
			foreach (FeatureStruct disjunct in _disjuncts)
				disjunct.FindReentrances(reentrances);
		}

		internal void GetAllValues(ISet<FeatureValue> values)
		{
			foreach (FeatureStruct disjunct in _disjuncts)
				disjunct.GetAllValues(values, true);
		}

		internal bool Equals(Disjunction other, ISet<FeatureValue> visitedSelf, ISet<FeatureValue> visitedOther, IDictionary<FeatureValue, FeatureValue> visitedPairs)
		{
			if (_disjuncts.Count != other._disjuncts.Count)
				return false;

			if (_disjuncts.Count > 0)
			{
				var matched = new HashSet<int>();
				foreach (FeatureStruct thisDisjunct in _disjuncts)
				{
					bool found = false;
					for (int i = 0; i < other._disjuncts.Count; i++)
					{
						if (matched.Contains(i))
							continue;

						var comparer = new ReferenceEqualityComparer<FeatureValue>();
						var tempVisitedSelf = new HashSet<FeatureValue>(visitedSelf, comparer);
						var tempVisitedOther = new HashSet<FeatureValue>(visitedOther, comparer);
						var tempVisitedPairs = new Dictionary<FeatureValue, FeatureValue>(visitedPairs, comparer);

						if (thisDisjunct.Equals(other._disjuncts[i], tempVisitedSelf, tempVisitedOther, tempVisitedPairs))
						{
							visitedSelf = tempVisitedSelf;
							visitedOther = tempVisitedOther;
							visitedPairs = tempVisitedPairs;
							matched.Add(i);
							found = true;
							break;
						}
					}

					if (!found)
						return false;
				}
			}

			return true;
		}

		internal int GetHashCode(ISet<FeatureValue> visited)
		{
			return _disjuncts.Aggregate(23, (code, fs) => code ^ fs.GetHashCode(visited));
		}

		internal string ToString(ISet<FeatureValue> visited, IDictionary<FeatureValue, int> reentranceIds)
		{
			var sb = new StringBuilder();
			bool first = true;
			foreach (FeatureStruct disjunct in _disjuncts)
			{
				if (!first)
					sb.Append(" || ");
				sb.Append(disjunct.ToString(visited, reentranceIds));
				first = false;
			}
			return sb.ToString();
		}
	}
}
