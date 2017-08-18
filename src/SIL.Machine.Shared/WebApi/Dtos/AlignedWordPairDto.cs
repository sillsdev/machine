namespace SIL.Machine.WebApi.Dtos
{
#if BRIDGE_NET
	[Bridge.ObjectLiteral(Bridge.ObjectInitializationMode.DefaultValue)]
#endif
	public class AlignedWordPairDto
	{
		public int SourceIndex { get; set; }
		public int TargetIndex { get; set; }
	}
}
