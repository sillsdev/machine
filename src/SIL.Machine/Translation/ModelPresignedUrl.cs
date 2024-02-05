namespace SIL.Machine.Translation
{
    public class ModelPresignedUrl
    {
        public string PresignedUrl { get; set; } = default;
        public int BuildRevision { get; set; } = default;
        public string UrlExpirationTime { get; set; } = default;
    }
}
