using System;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Server.DataAccess;
using SIL.Machine.WebApi.Server.Models;
using SIL.Machine.WebApi.Server.Utils;

namespace SIL.Machine.WebApi.Server.Services
{
	public class BuildProgress : IProgress<ProgressData>
	{
		private readonly IBuildRepository _buildRepo;
		private readonly Build _build;

		public BuildProgress(IBuildRepository buildRepo, Build build)
		{
			_buildRepo = buildRepo;
			_build = build;
		}

		public void Report(ProgressData value)
		{
			_build.StepCount = value.StepCount;
			_build.CurrentStep = value.CurrentStep;
			_build.CurrentStepMessage = value.CurrentStepMessage;
			_buildRepo.UpdateAsync(_build).WaitAndUnwrapException();
		}
	}
}
