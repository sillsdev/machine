using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine.Translation
{
    public interface IInteractiveTranslationModel : IInteractiveTranslationEngine, IWordAlignerEngine, ITranslationModel
    {
        Task SaveAsync(CancellationToken cancellationToken = default);
        void Save();
    }
}
