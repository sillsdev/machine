using System.Threading;
using System.Threading.Tasks;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Translation
{
    public class InteractiveTranslatorFactory
    {
        public InteractiveTranslatorFactory(IInteractiveTranslationEngine engine)
        {
            Engine = engine;
        }

        public IInteractiveTranslationEngine Engine { get; }
        public ErrorCorrectionModel ErrorCorrectionModel { get; } = new ErrorCorrectionModel();
        public IRangeTokenizer<string, int, string> TargetTokenizer { get; set; } = WhitespaceTokenizer.Instance;
        public IDetokenizer<string, string> TargetDetokenizer { get; set; } = WhitespaceDetokenizer.Instance;

        public async Task<InteractiveTranslator> CreateAsync(
            string segment,
            bool sentenceStart = true,
            CancellationToken cancellationToken = default
        )
        {
            return new InteractiveTranslator(
                ErrorCorrectionModel,
                Engine,
                TargetTokenizer,
                TargetDetokenizer,
                segment,
                await Engine.GetWordGraphAsync(segment, cancellationToken).ConfigureAwait(false),
                sentenceStart
            );
        }
    }
}
