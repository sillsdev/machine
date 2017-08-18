namespace SIL.Machine.WebApi.Dtos
{
#if BRIDGE_NET
	[Bridge.ObjectLiteral(Bridge.ObjectInitializationMode.DefaultValue)]
#endif
	public class EngineDto : LinkDto
	{
		public string Id { get; set; }
		public string SourceLanguageTag { get; set; }
		public string TargetLanguageTag { get; set; }
		public bool IsShared { get; set; }
		public LinkDto[] Projects { get; set; }
	}
}
