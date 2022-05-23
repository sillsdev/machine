namespace SIL.Machine.Morphology.HermitCrab
{
    public interface IMorphologicalRule : IHCRule
    {
        Stratum Stratum { get; set; }
    }
}
