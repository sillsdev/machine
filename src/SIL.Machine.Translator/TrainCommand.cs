namespace SIL.Machine.Translation
{
	public class TrainCommand : CommandBase
	{
		public TrainCommand()
		{
			Name = "train";

			AddSubcommand(new TrainTranslationModelCommand());
			AddSubcommand(new TrainAlignmentModelCommand());
		}
	}
}
