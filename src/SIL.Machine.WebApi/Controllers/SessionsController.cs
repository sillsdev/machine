using Microsoft.AspNetCore.Mvc;
using SIL.Machine.WebApi.Models;

namespace SIL.Machine.WebApi.Controllers
{
	[Route("translation/[controller]")]
	public class SessionsController : Controller
	{
		private readonly ISessionService _sessionService;

		public SessionsController(ISessionService sessionService)
		{
			_sessionService = sessionService;
		}

		[HttpGet("{id}")]
		public IActionResult Get(string id)
		{
			SessionDto session;
			if (_sessionService.TryGet(id, out session))
				return new ObjectResult(session);
			return NotFound();
		}

		[HttpDelete("{id}")]
		public IActionResult EndSession(string id)
		{
			if (_sessionService.Remove(id))
				return Ok();
			return NotFound();
		}

		[HttpPost("{id}/actions/start")]
		public IActionResult Translate(string id, [FromBody] string segment)
		{
			SuggestionDto suggestion;
			if (_sessionService.TryStartTranslation(id, segment, out suggestion))
				return new ObjectResult(suggestion);
			return NotFound();
		}

		[HttpPost("{id}/actions/update")]
		public IActionResult UpdatePrefix(string id, [FromBody] string prefix)
		{
			SuggestionDto suggestion;
			if (_sessionService.TryUpdatePrefix(id, prefix, out suggestion))
				return new ObjectResult(suggestion);
			return NotFound();
		}

		[HttpPost("{id}/actions/approve")]
		public IActionResult Approve(string id)
		{
			if (_sessionService.TryApprove(id))
				return Ok();
			return NotFound();
		}
	}
}
