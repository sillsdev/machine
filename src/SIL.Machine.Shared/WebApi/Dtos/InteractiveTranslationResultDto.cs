namespace SIL.Machine.WebApi.Dtos
{
#if BRIDGE_NET
	[Bridge.ObjectLiteral(Bridge.ObjectInitializationMode.DefaultValue)]
#endif
	public class InteractiveTranslationResultDto
	{
		public WordGraphDto WordGraph { get; set; }
		public TranslationResultDto RuleResult { get; set; }
	}
}
