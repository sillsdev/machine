using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
	public class SimpleContext
	{
		private readonly NaturalClass _nc;
		private readonly ReadOnlyCollection<SymbolicFeatureValue> _variables;
		private readonly FeatureStruct _fs;

		public SimpleContext(NaturalClass nc, IEnumerable<SymbolicFeatureValue> variables)
		{
			_nc = nc;
			_variables = new ReadOnlyCollection<SymbolicFeatureValue>(variables.ToArray());
			_fs = _nc.FeatureStruct.DeepClone();
			foreach (SymbolicFeatureValue var in _variables)
				_fs.AddValue(var.Feature, var);
			_fs.Freeze();
		}

		public NaturalClass NaturalClass
		{
			get { return _nc; }
		}

		public ReadOnlyCollection<SymbolicFeatureValue> Variables
		{
			get { return _variables; }
		}

		public FeatureStruct FeatureStruct
		{
			get { return _fs; }
		}

		public override string ToString()
		{
			return _fs.ToString();
		}
	}
}
