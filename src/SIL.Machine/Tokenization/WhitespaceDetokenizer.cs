namespace SIL.Machine.Tokenization
{
    public class WhitespaceDetokenizer : StringDetokenizer
    {
        public static WhitespaceDetokenizer Instance { get; } = new WhitespaceDetokenizer();

        protected override DetokenizeOperation GetOperation(object context, string token)
        {
            return DetokenizeOperation.NoOperation;
        }
    }
}
