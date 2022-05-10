namespace SIL.Machine.WebApi.Services;

public interface IPretranslationService
{
	Task<IEnumerable<Pretranslation>> GetAllAsync(string engineId, string corpusId);
	Task<IEnumerable<Pretranslation>> GetAllAsync(string engineId, string corpusId, string textId);
}
