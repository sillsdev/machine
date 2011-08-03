namespace SIL.APRE.Fsa
{
	public class FsaMatch<TOffset>
	{
		private readonly NullableValue<TOffset>[,] _registers;

		public FsaMatch(NullableValue<TOffset>[,] registers)
		{
			_registers = registers;
		}

		public NullableValue<TOffset>[,] Registers
		{
			get { return _registers; }
		}
	}
}
