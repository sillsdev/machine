namespace SIL.Machine.Translation
{
    public class LanguageInfoDto
    {
        public LanguageInfoDto(
            string language,
            string engineType,
            string isoLanguageCode,
            string commonLanguageName,
            bool nativeLanguageSupport
        )
        {
            Language = language;
            EngineType = engineType;
            ISOLanguageCode = isoLanguageCode;
            CommonLanguageName = commonLanguageName;
            NativeLanguageSupport = nativeLanguageSupport;
        }

        public string Language { get; set; } = default;
        public string EngineType { get; set; } = default;
        public string ISOLanguageCode { get; set; } = default;
        public string CommonLanguageName { get; set; } = default;
        public bool NativeLanguageSupport { get; set; } = default;
    }
}
