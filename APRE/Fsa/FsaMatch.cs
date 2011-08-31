using SIL.APRE.FeatureModel;

namespace SIL.APRE.Fsa
{
	public class FsaMatch<TOffset>
	{
		private readonly NullableValue<TOffset>[,] _registers;
		private readonly VariableBindings _varBindings;
		private readonly string _id;

		public FsaMatch(string id, NullableValue<TOffset>[,] registers, VariableBindings varBindings)
		{
			_id = id;
			_registers = registers;
			_varBindings = varBindings;
		}

		public string ID
		{
			get { return _id; }
		}

		public NullableValue<TOffset>[,] Registers
		{
			get { return _registers; }
		}

		public VariableBindings VariableBindings
		{
			get { return _varBindings; }
		}
	}
}
