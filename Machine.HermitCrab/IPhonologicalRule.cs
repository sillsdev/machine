using SIL.Machine.DataStructures;

namespace SIL.Machine.HermitCrab
{
	public interface IPhonologicalRule : IHCRule
	{
		Direction Direction { get; set; }
	}
}
