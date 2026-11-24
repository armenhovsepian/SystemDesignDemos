using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;
using PreSignedUrlDemo.Models;

namespace PreSignedUrlDemo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideosController : ControllerBase
    {
        private readonly BlobServiceClient _blobServiceClient;
        public VideosController(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }

        [HttpPost("upload-url")]
        public async Task<IActionResult> UploadUrl([FromBody] UploadRequest request)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("videos");
            await containerClient.CreateIfNotExistsAsync();

            var blobName = $"{Guid.NewGuid()}/{Path.GetFileName(request.FileName)}";
            var blobClient = containerClient.GetBlobClient(blobName);

            // A pre-signed URL called a SAS (Shared Access Signature) URL in Azure Blob Storage
            // Build a SAS token allowing Create and Write for 10 minutes
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = containerClient.Name,
                BlobName = blobClient.Name,
                Resource = "b", // "b" = blob
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(10)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

            // Generates a URL like: http://127.0.0.1:10000/devstoreaccount1/videos/guid/video.mp4?sv=...&sig=...
            Uri sasUri = blobClient.GenerateSasUri(sasBuilder);

            return Ok(new { uploadUrl = sasUri.ToString(), blobName });
        }
    }
}
