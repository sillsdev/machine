namespace SIL.Machine.WebApi.Services;

public class PretranslationService : EntityServiceBase<Pretranslation>, IPretranslationService
{
	public PretranslationService(IRepository<Pretranslation> pretranslations)
		: base(pretranslations)
	{
	}

	public async Task<IEnumerable<Pretranslation>> GetAllAsync(string engineId, string corpusId)
	{
		return await Entities.GetAllAsync(pt => pt.TranslationEngineRef == engineId && pt.CorpusRef == engineId);
	}

	public async Task<IEnumerable<Pretranslation>> GetAllAsync(string engineId, string corpusId, string textId)
	{
		return await Entities.GetAllAsync(pt => pt.TranslationEngineRef == engineId && pt.CorpusRef == engineId
			&& pt.TextId == textId);
	}
}
