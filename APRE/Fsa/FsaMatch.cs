namespace SIL.APRE.Fsa
{
	public class FsaMatch<TOffset, TData>
	{
		private readonly NullableValue<TOffset>[,] _registers;
		private readonly TData _data;

		public FsaMatch(NullableValue<TOffset>[,] registers, TData data)
		{
			_registers = registers;
			_data = data;
		}

		public NullableValue<TOffset>[,] Registers
		{
			get { return _registers; }
		}

		public TData Data
		{
			get { return _data; }
		}
	}
}
