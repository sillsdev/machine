using System.Collections.Generic;

namespace SIL.Machine.WebApi.Services
{
	public class WordGraphDto
	{
		public float InitialStateScore { get; set; }
		public IReadOnlyList<int> FinalStates { get; set; }
		public IReadOnlyList<WordGraphArcDto> Arcs { get; set; }
	}
}
