namespace SIL.Machine.AspNetCore.Models
{
    public class LanguageInfo
    {
        public bool IsNative { get; set; } = default!;
        public string? InternalCode { get; set; } = default!;
        public string? Name { get; set; } = default!;
    }
}
