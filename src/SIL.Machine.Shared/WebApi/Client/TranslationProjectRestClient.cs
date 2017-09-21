using Newtonsoft.Json;
using SIL.Machine.WebApi.Dtos;
using System.Threading.Tasks;

namespace SIL.Machine.WebApi.Client
{
	public class TranslationProjectRestClient : RestClientBase
	{
		public TranslationProjectRestClient(string baseUrl, IHttpClient httpClient)
			: base(baseUrl, httpClient)
		{
		}

		public async Task<ProjectDto> GetProjectAsync(string projectId)
		{
			string url = $"translation/projects/id:{projectId}";
			HttpResponse response = await HttpClient.SendAsync(HttpRequestMethod.Get, url);
			if (!response.IsSuccess)
				throw new HttpException("Error getting project.") { StatusCode = response.StatusCode };
			return JsonConvert.DeserializeObject<ProjectDto>(response.Content, SerializerSettings);
		}
	}
}
