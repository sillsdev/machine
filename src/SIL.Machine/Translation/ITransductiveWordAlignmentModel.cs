namespace SIL.Machine.Translation
{
    public interface ITransductiveWordAlignmentModel
    {
        int TrainingAlignmentCount { get; }
        WordAlignmentMatrix GetTrainingAlignment(int n);
    }
}
