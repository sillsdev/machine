namespace SIL.Machine.WebApi
{
	public class WordGraphDto
	{
		public float InitialStateScore { get; set; }
		public int[] FinalStates { get; set; }
		public WordGraphArcDto[] Arcs { get; set; }
	}
}
