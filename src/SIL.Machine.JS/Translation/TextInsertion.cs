using Bridge;

namespace SIL.Machine.Translation
{
	[ObjectLiteral(ObjectInitializationMode.DefaultValue)]
	public class TextInsertion
	{
		public int DeleteLength { get; set; }
		public string InsertText { get; set; }
	}
}
