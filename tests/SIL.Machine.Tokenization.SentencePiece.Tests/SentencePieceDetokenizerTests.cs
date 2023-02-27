using NUnit.Framework;

namespace SIL.Machine.Tokenization.SentencePiece
{
    [TestFixture]
    public class SentencePieceDetokenizerTests
    {
        [Test]
        public void Detokenize()
        {
            var detokenizer = new SentencePieceDetokenizer();
            string sentence = detokenizer.Detokenize(
                "▁In ▁particular , ▁the ▁actress es ▁play ▁a ▁major ▁role ▁in ▁the ▁sometimes ▁rather ▁dubious ▁staging .".Split()
            );
            Assert.That(
                sentence,
                Is.EqualTo("In particular, the actresses play a major role in the sometimes rather dubious staging.")
            );
        }

        [Test]
        public void Detokenize_Empty()
        {
            var detokenizer = new SentencePieceDetokenizer();
            string sentence = detokenizer.Detokenize(Enumerable.Empty<string>());
            Assert.That(sentence, Is.EqualTo(""));
        }
    }
}
