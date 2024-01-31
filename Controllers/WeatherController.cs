using Azure;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AzureWebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherController> _logger;
        private readonly IOptions<WeatherConfigOptions> _options;
        private readonly BlobServiceClient _blobServiceClient;

        public WeatherController(
            ILogger<WeatherController> logger,
            IOptions<WeatherConfigOptions> options,
            BlobServiceClient blobServiceClient)
        {
            _logger = logger;
            _options = options;
            _blobServiceClient = blobServiceClient;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        [HttpGet("file")]
        public async Task<IActionResult> GetFile(string name)
        {
            try
            {
                var blobContainerClient = _blobServiceClient.GetBlobContainerClient(_options.Value.BlobStorageContainerName);
                var blobClient = blobContainerClient.GetBlobClient(name);
                var result = await blobClient.DownloadStreamingAsync();

                return File(result.Value.Content, result.Value.Details.ContentType);
            }
            catch (RequestFailedException ex)
            {
                return NotFound(ex.ErrorCode);
            }
        }
    }
}
