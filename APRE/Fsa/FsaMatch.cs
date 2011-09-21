using SIL.APRE.FeatureModel;

namespace SIL.APRE.Fsa
{
	public class FsaMatch<TOffset>
	{
		private readonly NullableValue<TOffset>[,] _registers;
		private readonly VariableBindings _varBindings;
		private readonly string _id;
		private readonly int _priority;

		private readonly bool _isLazy;

		internal FsaMatch(string id, NullableValue<TOffset>[,] registers, VariableBindings varBindings, int priority, bool isLazy)
		{
			_id = id;
			_registers = registers;
			_varBindings = varBindings;
			_priority = priority;
			_isLazy = isLazy;
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

		public int Priority
		{
			get { return _priority; }
		}

		public bool IsLazy
		{
			get { return _isLazy; }
		}
	}
}
