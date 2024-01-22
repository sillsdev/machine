namespace SIL.Machine.Translation
{
    public class LanguageInfoDto
    {
        public LanguageInfoDto(string language, string engineType, string internalCode, string name, bool isNative)
        {
            Language = language;
            EngineType = engineType;
            InternalCode = internalCode;
            Name = name;
            IsNative = isNative;
        }

        public string Language { get; set; } = default;
        public string EngineType { get; set; } = default;
        public string InternalCode { get; set; } = default;
        public string Name { get; set; } = default;
        public bool IsNative { get; set; } = default;
    }
}
