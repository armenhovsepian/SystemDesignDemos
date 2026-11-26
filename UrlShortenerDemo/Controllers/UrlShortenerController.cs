using Microsoft.AspNetCore.Mvc;
using UrlShortenerDemo.Models;
using UrlShortenerDemo.Services;

namespace UrlShortenerDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UrlShortenerController : ControllerBase
    {

        private readonly UrlShortenerService _service;

        public UrlShortenerController(UrlShortenerService service) => _service = service;

        [HttpPost("shorten")]
        public async Task<IActionResult> Shorten([FromBody] ShortenRequest req)
        {
            if (!Uri.TryCreate(req.Url, UriKind.Absolute, out _))
                return BadRequest("Invalid URL");

            var code = await _service.ShortenAsync(req.Url, req.IsPermanent, req.ExpiresOn);
            var shortUrl = $"{Request.Scheme}://{Request.Host}/{code}";
            return Ok(new { shortUrl, code });
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> Redirection(string code)
        {
            var entry = await _service.ResolveAsync(code);

            if (entry is null)
                return NotFound();

            if (entry.ExpiresOn.HasValue && entry.ExpiresOn < DateTime.UtcNow)
                return Gone(); // 410

            // Fire-and-forget click tracking
            _ = Task.Run(() => _service.TrackClickAsync(entry.Id));

            return entry.IsPermanent
                ? RedirectPermanent(entry.OriginalUrl)
                : Redirect(entry.OriginalUrl);

        }

        private IActionResult Gone() => StatusCode(410);
    }
}
