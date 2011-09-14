using System.Collections.Generic;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Fsa
{
	public class ArcCondition<TOffset>
	{
		private readonly FeatureStruct _fs;

		public ArcCondition(FeatureStruct fs)
		{
			_fs = fs;
		}

		public FeatureStruct FeatureStruct
		{
			get { return _fs; }
		}

		public bool IsMatch(Annotation<TOffset> ann, VariableBindings varBindings)
		{
			if (ann == null)
				return false;

			return ann.FeatureStruct.IsUnifiable(_fs, false, varBindings);
		}

		public bool Negation(out ArcCondition<TOffset> output)
		{
			FeatureStruct fs;
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
			FeatureStruct fs;
			if (!cond._fs.Unify(_fs, false, new VariableBindings(), false, out fs))
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
