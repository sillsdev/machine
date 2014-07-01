using SIL.Collections;

namespace SIL.HermitCrab
{
	public interface IPhonologicalRule : IHCRule
	{
		Direction Direction { get; set; }
	}
}
