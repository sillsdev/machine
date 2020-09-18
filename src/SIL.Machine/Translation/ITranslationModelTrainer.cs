namespace SIL.Machine.Translation
{
	public interface ITranslationModelTrainer : ITrainer
	{
		SmtBatchTrainStats Stats { get; }
	}
}
