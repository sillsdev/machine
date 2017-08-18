namespace SIL.Machine.WebApi.Dtos
{
#if BRIDGE_NET
	[Bridge.ObjectLiteral(Bridge.ObjectInitializationMode.DefaultValue)]
#endif
	public class WordGraphDto
	{
		public float InitialStateScore { get; set; }
		public int[] FinalStates { get; set; }
		public WordGraphArcDto[] Arcs { get; set; }
	}
}
