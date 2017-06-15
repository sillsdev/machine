using System;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.DataAccess;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.Services
{
	public class BuildProgress : IProgress<SmtTrainProgress>
	{
		private readonly IBuildRepository _buildRepo;
		private readonly Build _build;

		public BuildProgress(IBuildRepository buildRepo, Build build)
		{
			_buildRepo = buildRepo;
			_build = build;
		}

		public void Report(SmtTrainProgress value)
		{
			_build.StepCount = value.StepCount;
			_build.CurrentStep = value.CurrentStep;
			_build.CurrentStepMessage = value.CurrentStepMessage;
			_buildRepo.Update(_build);
		}
	}
}
