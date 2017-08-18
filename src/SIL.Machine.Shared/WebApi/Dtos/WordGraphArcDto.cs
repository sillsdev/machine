namespace SIL.Machine.WebApi.Dtos
{
#if BRIDGE_NET
	[Bridge.ObjectLiteral(Bridge.ObjectInitializationMode.DefaultValue)]
#endif
	public class WordGraphArcDto
	{
		public int PrevState { get; set; }
		public int NextState { get; set; }
		public float Score { get; set; }
		public string[] Words { get; set; }
		public float[] Confidences { get; set; }
		public int SourceStartIndex { get; set; }
		public int SourceEndIndex { get; set; }
		public bool IsUnknown { get; set; }
		public AlignedWordPairDto[] Alignment { get; set; }
	}
}
