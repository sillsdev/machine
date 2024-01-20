namespace SIL.Machine.Translation
{
    public class LanguageInfoDto
    {
        public LanguageInfoDto(
            string languageCode,
            string engineType,
            string resolvedLanguageCode,
            string commonLanguageName,
            bool nativeLanguageSupport
        )
        {
            LanguageCode = languageCode;
            EngineType = engineType;
            ResolvedLanguageCode = resolvedLanguageCode;
            CommonLanguageName = commonLanguageName;
            NativeLanguageSupport = nativeLanguageSupport;
        }

        public string LanguageCode { get; set; } = default;
        public string EngineType { get; set; } = default;
        public string ResolvedLanguageCode { get; set; } = default;
        public string CommonLanguageName { get; set; } = default;
        public bool NativeLanguageSupport { get; set; } = default;
    }
}
