using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using UrlShortenerDemo.Data;
using UrlShortenerDemo.Entities;

namespace UrlShortenerDemo.Services
{
    public class UrlShortenerService
    {

        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;

        private readonly AppSettings _settings;

        public UrlShortenerService(IOptions<AppSettings> options, IMemoryCache cache, AppDbContext db) 
        {
            _db = db;
            _settings = options.Value;
            _cache = cache;
        }

        public async Task<string> ShortenAsync(string originalUrl, bool permanent = false,
            DateTime? expiresOn = null)
        {

            // Check if already shortened (non-expired only)
            var existing = await _db.ShortenedUrls.AsNoTracking()
                .FirstOrDefaultAsync(u => u.OriginalUrl == originalUrl
                                       && (u.ExpiresOn == null || u.ExpiresOn > DateTime.UtcNow));

            if (existing != null)
                return existing.Code;

            var entry = new ShortenedUrl
            {
                OriginalUrl = originalUrl,
                CreatedOn = DateTime.UtcNow,
                ExpiresOn = expiresOn,
                IsPermanent = permanent,
                Code = string.Empty   // placeholder — filled below
            };

            await using var tx = await _db.Database.BeginTransactionAsync();

            _db.ShortenedUrls.Add(entry);
            await _db.SaveChangesAsync();         // DB assigns entry.Id here

            // Convert ID to short url, prefixed with InstanceId to avoid collisions across different instances
            entry.Code = _settings.InstanceId + Base62.Encode(entry.Id); // e.g. Id=1000 → "i1g8"
            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            _cache.Set(entry.Code, entry, TimeSpan.FromMinutes(30));

            return entry.Code;
        }


        public async Task<ShortenedUrl?> ResolveAsync(string code)
        {
            if (_cache.TryGetValue(code, out ShortenedUrl? cached))
                return cached;

            var entry = await _db.ShortenedUrls.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Code == code);

            if (entry != null)
                _cache.Set(code, entry, TimeSpan.FromMinutes(30));

            return entry;
        }
    }
}
