using SIL.CommandLine;
using SIL.Machine.Translation.Thot;
using System;
using System.IO;

namespace SIL.Machine.Translation
{
	public class TrainCommand : ParallelTextCommand
	{
		public TrainCommand()
			: base(true)
		{
			Name = "train";
		}

		protected override int ExecuteCommand()
		{
			int code = base.ExecuteCommand();
			if (code != 0)
				return code;

			if (!Directory.Exists(EngineDirectory))
				Directory.CreateDirectory(EngineDirectory);

			if (!File.Exists(EngineConfigFileName))
			{
				string defaultConfigFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data",
					"default-smt.cfg");
				File.Copy(defaultConfigFileName, EngineConfigFileName);
			}

			ISmtBatchTrainer trainer = new ThotSmtBatchTrainer(EngineConfigFileName, Preprocessors.Lowercase,
				SourceCorpus, Preprocessors.Lowercase, TargetCorpus, AlignmentsCorpus);

			Out.Write("Training... ");
			using (var progress = new ConsoleProgressBar<SmtTrainProgress>(Out,
				p => (double) p.CurrentStep / p.StepCount))
			{
				trainer.Train(progress);
				trainer.Save();
			}
			Out.WriteLine("done.");

			return 0;
		}
	}
}
