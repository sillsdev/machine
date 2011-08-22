using System.Collections.Generic;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Fsa
{
	public class ArcCondition<TOffset>
	{
		private readonly FeatureStructure _fs;

		public ArcCondition(FeatureStructure fs)
		{
			_fs = fs;
		}

		public FeatureStructure FeatureStructure
		{
			get { return _fs; }
		}

		public bool IsMatch(Annotation<TOffset> ann, IDictionary<string, FeatureValue> varBindings)
		{
			if (ann == null)
				return false;

			return ann.FeatureStructure.IsUnifiable(_fs, false, false, varBindings);
		}

		public bool Negation(out ArcCondition<TOffset> output)
		{
			FeatureStructure fs;
			if (!_fs.Negation(out fs))
			{
				output = null;
				return false;
			}

			output = new ArcCondition<TOffset>(fs);
			return true;
		}

		public bool Conjunction(ArcCondition<TOffset> cond, out ArcCondition<TOffset> output)
		{
			FeatureStructure fs;
			if (!cond._fs.Unify(_fs, false, false, out fs))
			{
				output = null;
				return false;
			}

			output = new ArcCondition<TOffset>(fs);
			return true;
		}

		public override string ToString()
		{
			return _fs.ToString();
		}
	}
}
