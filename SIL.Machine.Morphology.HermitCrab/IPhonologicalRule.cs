using SIL.Machine.DataStructures;

namespace SIL.Machine.Morphology.HermitCrab
{
	public interface IPhonologicalRule : IHCRule
	{
		Direction Direction { get; set; }
	}
}
