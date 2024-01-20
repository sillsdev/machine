namespace SIL.Machine.AspNetCore.Services;

public interface ILanguageTagService
{
    bool ConvertToFlores200Code(string languageTag, out string flores200Code);
}
