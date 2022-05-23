using System.Threading.Tasks;

namespace SIL.Machine.Translation
{
    public interface IInteractiveTranslationModel : ITranslationModel
    {
        IInteractiveTranslationEngine CreateInteractiveEngine();

        void Save();
        Task SaveAsync();
    }
}
