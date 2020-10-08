namespace SIL.Machine
{
	public class TrainCommand : CommandBase
	{
		public TrainCommand()
		{
			Name = "train";
			Description = "Trains a model.";

			AddCommand(new TrainTranslationModelCommand());
			AddCommand(new TrainAlignmentModelCommand());
		}
	}
}
