namespace SIL.Machine.AspNetCore.Services;

public interface ILanguageTagService
{
    string ConvertToFlores200Code(string languageTag);
    Models.LanguageInfo GetFlores200LanguageInfo(string languageTag);
}
