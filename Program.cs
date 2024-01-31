using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace AzureWebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            
            builder.Services.AddControllers();
            
            builder.Services.AddOptions<AzureOptions>().Bind(builder.Configuration.GetSection("Azure"));
            builder.Services.AddOptions<WeatherConfigOptions>().Configure(options =>
            {
                options.BlobStorageContainerName = builder.Configuration["Azure:BlobStorageContainerName"];
            });

            // Main rule: The main rule of Azure SDK client lifetime management is: treat clients as singletons.
            // see https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-client-management?tabs=dotnet#manage-client-objects
            // and: https://devblogs.microsoft.com/azure-sdk/lifetime-management-and-thread-safety-guarantees-of-azure-sdk-net-clients/

            // for Identity, see the chain for Default here
            // https://learn.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme?view=azure-dotnet&preserve-view=true#key-concepts
            // also see https://www.domstamand.com/migrating-to-the-new-csharp-azure-keyvault-sdk-libraries/ for help on Azure.Identity and new SDK
            // use includeInteractiveCredentials: true which will interactively authenticate the developer via the current system's default browser
            TokenCredential tokenCredential;
            if (builder.Environment.IsDevelopment())
            {
                tokenCredential = new DefaultAzureCredential();
            }
            else
            {
                var userManageIdentityClientId = builder.Configuration["Azure:UserManageIdentityClientId"];
                tokenCredential =
                    new ChainedTokenCredential(
                        // uses env vars: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme?view=azure-dotnet&preserve-view=true#environment-variables
                        new EnvironmentCredential(),
                        // AKS
                        new WorkloadIdentityCredential(),
                        // Managed Identity
                        string.IsNullOrWhiteSpace(userManageIdentityClientId) ? new ManagedIdentityCredential() : new ManagedIdentityCredential(userManageIdentityClientId));
            }

            // either this using package Microsoft.Extensions.Azure with the help of additional Azure registration methods
            /*builder.Services.AddAzureClients(b =>
            {
                var storageAccountEndpoint = builder.Configuration["Azure:StorageAccountEndPoint"];
                var storageAccountConnectionString = builder.Configuration["Azure:StorageAccountAccessKey"];

                if (string.IsNullOrWhiteSpace(storageAccountConnectionString))
                {
                    b.AddBlobServiceClient(new Uri(storageAccountEndpoint))
                        .WithCredential(tokenCredential);
                    
                    // or use this for all clients
                    // b.UseCredential(tokenCredential);
                }
                else
                {
                    b.AddBlobServiceClient(storageAccountConnectionString);
                }
            });*/

            // or like this
            builder.Services.AddSingleton<BlobServiceClient>(sp =>
            {
                var azureConfig = sp.GetRequiredService<IOptions<AzureOptions>>().Value;
                if (string.IsNullOrWhiteSpace(azureConfig.StorageAccountConnectionString))
                {
                    return new BlobServiceClient(new Uri(azureConfig.StorageAccountEndPoint), tokenCredential);
                }

                return new BlobServiceClient(azureConfig.StorageAccountConnectionString);

            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
