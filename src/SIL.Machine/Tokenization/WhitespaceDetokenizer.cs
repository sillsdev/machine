namespace SIL.Machine.Tokenization
{
    public class WhitespaceDetokenizer : StringDetokenizer
    {
        public static WhitespaceDetokenizer Instance { get; } = new WhitespaceDetokenizer();

        protected override DetokenizeOperation GetOperation(object ctxt, string token)
        {
            return DetokenizeOperation.NoOperation;
        }
    }
}
