namespace SIL.Machine.WebApi.Dtos
{
#if BRIDGE_NET
	[Bridge.ObjectLiteral(Bridge.ObjectInitializationMode.DefaultValue)]
#endif
	public class SegmentPairDto
	{
		public string[] SourceSegment { get; set; }
		public string[] TargetSegment { get; set; }
	}
}
