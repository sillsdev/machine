using SIL.Machine.WebApi.Client;
using SIL.Machine.WebApi.Dtos;
using System;

namespace SIL.Machine.Project
{
	public class TranslationProjectManager
	{
		private readonly TranslationProjectRestClient _restClient;

		public TranslationProjectManager(string baseUrl, IHttpClient httpClient = null)
		{
			_restClient = new TranslationProjectRestClient(baseUrl, httpClient ?? new AjaxHttpClient());
		}

		public void GetProject(string projectId, Action<ProjectDto> onFinished)
		{
			_restClient.GetProjectAsync(projectId).ContinueWith(t => onFinished(t.IsFaulted ? null : t.Result));
		}
	}
}
