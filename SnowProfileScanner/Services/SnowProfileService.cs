using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using SnowProfileScanner.Models;

namespace SnowProfileScanner.Services
{
    public class SnowProfileService
    {
        private readonly IConfiguration _configuration;

        public SnowProfileService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<SnowProfileEntity> UploadProfile(
            string name,
            MemoryStream memoryStream,
            MemoryStream? plotStream,
            SnowProfile snowProfile
        ) {
            using var uploadStream = new MemoryStream(memoryStream.ToArray());
            string connectionString = _configuration["AzureStorageConnectionString"];

            BlobClient blobClient = await UploadImage(uploadStream, connectionString, "image/jpeg");
            BlobClient? plotClient = null;

            if (plotStream is not null)
            {
                using MemoryStream uploadPlotStream = new(plotStream.ToArray());
                plotClient = await UploadImage(uploadPlotStream, connectionString, "image/png");
            }

            var snowProfileEntity = new SnowProfileEntity
            {
                PartitionKey = name,
                RowKey = Guid.NewGuid().ToString(),
                ImageUrl = blobClient.Uri.ToString(),
                PlotUrl = plotClient?.Uri?.ToString(),
                Name = name,
                SnowProfile = snowProfile
            };

            // Save snowProfileEntity to Azure Table Storage
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("SnowProfiles");
            await table.CreateIfNotExistsAsync();

            TableOperation insertOperation = TableOperation.Insert(snowProfileEntity);
            await table.ExecuteAsync(insertOperation);
            return snowProfileEntity;
        }

        private static async Task<BlobClient> UploadImage(
            MemoryStream uploadStream,
            string connectionString,
            string contentType
        ) {
            string containerName = "snowprofilepictures";
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            string blobName = Guid.NewGuid().ToString();
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            BlobUploadOptions options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType,
                }
            };

            await blobClient.UploadAsync(uploadStream, options);
            return blobClient;
        }

        public async Task<List<SnowProfileEntity>> GetAll()
        {
            string connectionString = _configuration["AzureStorageConnectionString"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("SnowProfiles");

            await table.CreateIfNotExistsAsync();

            var query = new TableQuery<SnowProfileEntity>();
            var tableQuerySegment = await table.ExecuteQuerySegmentedAsync(query, null);
            var results = tableQuerySegment.Results;
            return results;
        }

        public async Task<SnowProfileEntity?> Get(string id)
        {
            string connectionString = _configuration["AzureStorageConnectionString"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("SnowProfiles");

            await table.CreateIfNotExistsAsync();

            var partitionFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, id);
            var query = new TableQuery<SnowProfileEntity>().Where(partitionFilter);

            var tableQuerySegment = await table.ExecuteQuerySegmentedAsync(query, null);
            var result = tableQuerySegment.Results;
            var temperatureProfileEntity = result.FirstOrDefault(); 
            return temperatureProfileEntity;
        }

    }
}
