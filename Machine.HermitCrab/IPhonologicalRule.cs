using SIL.Collections;

namespace SIL.Machine.HermitCrab
{
	public interface IPhonologicalRule : IHCRule
	{
		Direction Direction { get; set; }
	}
}
