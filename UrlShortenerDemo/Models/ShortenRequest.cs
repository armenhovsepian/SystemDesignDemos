namespace UrlShortenerDemo.Models
{
    public class ShortenRequest
    {
        public string Url { get; set; } = null!;
        public string? Title { get; set; }
        public DateTime? ExpiresOn { get; set; }
        public bool IsPermanent { get; set; }             // 301 vs 302
        public string? CreatedByUserId { get; set; }
    }
}
