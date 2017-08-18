namespace SIL.Machine.WebApi.Dtos
{
#if BRIDGE_NET
	[Bridge.ObjectLiteral(Bridge.ObjectInitializationMode.DefaultValue)]
#endif
	public class LinkDto
	{
		public string Href { get; set; }
	}
}
