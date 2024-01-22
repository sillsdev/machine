namespace SIL.Machine.Translation
{
    public class LanguageInfoDto
    {
        public LanguageInfoDto(
            string language,
            string engineType,
            string internalCode,
            string commonLanguageName,
            bool isSupportedNatively
        )
        {
            Language = language;
            EngineType = engineType;
            InternalCode = internalCode;
            CommonLanguageName = commonLanguageName;
            IsSupportedNatively = isSupportedNatively;
        }

        public string Language { get; set; } = default;
        public string EngineType { get; set; } = default;
        public string InternalCode { get; set; } = default;
        public string CommonLanguageName { get; set; } = default;
        public bool IsSupportedNatively { get; set; } = default;
    }
}
