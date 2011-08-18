using System.Collections.Generic;

namespace SIL.APRE.Fsa
{
	public class FsaMatch<TOffset>
	{
		private readonly NullableValue<TOffset>[,] _registers;
		private readonly IDictionary<string, FeatureValue> _varBindings; 

		public FsaMatch(NullableValue<TOffset>[,] registers, IDictionary<string, FeatureValue> varBindings)
		{
			_registers = registers;
			_varBindings = varBindings;
		}

		public NullableValue<TOffset>[,] Registers
		{
			get { return _registers; }
		}

		public IDictionary<string, FeatureValue> VariableBindings
		{
			get { return _varBindings; }
		}
	}
}
