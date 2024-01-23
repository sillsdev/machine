namespace SIL.Machine.AspNetCore.Models
{
    public class LanguageInfoDto
    {
        public string Language { get; set; } = default!;
        public string EngineType { get; set; } = default!;
        public bool IsNative { get; set; } = default!;
        public string? InternalCode { get; set; } = default!;
        public string? Name { get; set; } = default!;
    }
}
