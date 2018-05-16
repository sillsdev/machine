using System;
using SIL.Machine.Translation;
using SIL.Machine.WebApi.Server.DataAccess;
using SIL.Machine.WebApi.Server.Models;
using SIL.Machine.WebApi.Server.Utils;

namespace SIL.Machine.WebApi.Server.Services
{
	public class BuildProgress : IProgress<ProgressStatus>
	{
		private readonly IBuildRepository _buildRepo;
		private readonly Build _build;

		public BuildProgress(IBuildRepository buildRepo, Build build)
		{
			_buildRepo = buildRepo;
			_build = build;
		}

		public void Report(ProgressStatus value)
		{
			_build.PercentCompleted = value.PercentCompleted;
			_build.Message = value.Message;
			_buildRepo.UpdateAsync(_build).WaitAndUnwrapException();
		}
	}
}
