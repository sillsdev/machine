using System.Threading;
using System.Threading.Tasks;

namespace SIL.Machine.Translation
{
    public interface IInteractiveTranslationModel : IInteractiveTranslationEngine, ITranslationModel
    {
        Task SaveAsync(CancellationToken cancellationToken = default);
    }
}
