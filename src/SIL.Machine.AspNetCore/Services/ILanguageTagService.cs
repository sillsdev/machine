namespace SIL.Machine.AspNetCore.Services;

public interface ILanguageTagService
{
    string ConvertToFlores200Code(string languageTag);
    LanguageInfoDto CheckInFlores200(string languageTag);
}
