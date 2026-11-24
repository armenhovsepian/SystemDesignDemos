
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace PreSignedUrlDemo
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddSingleton(_ =>
                new BlobServiceClient(builder.Configuration.GetConnectionString("AzureStorage")));

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            // Configure Azurite CORS on startup in Development
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();

                var blobServiceClient = app.Services.GetRequiredService<BlobServiceClient>();
                var props = (await blobServiceClient.GetPropertiesAsync()).Value;
                props.Cors.Clear();
                props.Cors.Add(new BlobCorsRule
                {
                    AllowedOrigins = "*",
                    AllowedMethods = "PUT,OPTIONS,GET,HEAD",
                    AllowedHeaders = "*",
                    ExposedHeaders = "*",
                    MaxAgeInSeconds = 3600
                });
                await blobServiceClient.SetPropertiesAsync(props);
            }
            else
            {
                // Only force HTTPS in non-development so local HTTP can talk to HTTP Azurite
                app.UseHttpsRedirection();
            }

            app.UseAuthorization();

            app.UseDefaultFiles(); // Rewrites "/" to "/index.html"
            app.UseStaticFiles();  // Serves files from wwwroot

            app.MapControllers();

            app.Run();
        }
    }
}
