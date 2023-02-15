namespace Serval.AspNetCore.Services;

public interface IPretranslationService
{
    Task<IEnumerable<Pretranslation>> GetAllAsync(string engineId, string corpusId);
    Task<IEnumerable<Pretranslation>> GetAllAsync(string engineId, string corpusId, string textId);
}
