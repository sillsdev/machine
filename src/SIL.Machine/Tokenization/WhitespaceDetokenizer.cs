namespace SIL.Machine.Tokenization
{
	public class WhitespaceDetokenizer : StringDetokenizer
	{
		protected override DetokenizeOperation GetOperation(object ctxt, string token)
		{
			return DetokenizeOperation.NoOperation;
		}
	}
}
