namespace UrlShortenerDemo.Entities
{
    public class ShortenedUrl
    {
        public long Id { get; set; }
        public string Code { get; set; } = null!;        // e.g. "aB3xZ9"
        public string OriginalUrl { get; set; } = null!;
        public string? Title { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? ExpiresOn { get; set; }
        public bool IsPermanent { get; set; }             // 301 vs 302
        public int ClickCount { get; set; }
        public string? CreatedByUserId { get; set; }
    }
}
