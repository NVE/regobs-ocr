using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using SnowProfileScanner.Models;

namespace SnowProfileScanner.Services
{
    public class TemperatureProfileService
    {
        private readonly IConfiguration _configuration;

        public TemperatureProfileService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task UploadProfile(string name, MemoryStream memoryStream, TemperatureProfile temperatureProfile)
        {
            using (var uploadStream = new MemoryStream(memoryStream.ToArray()))
            {
                // Upload image to Azure Blob Storage
                string connectionString = _configuration["AzureStorageConnectionString"];

                BlobClient blobClient = await UploadImage(uploadStream, connectionString);

                var temperatureProfileEntity = new TemperatureProfileEntity
                {
                    PartitionKey = name,
                    RowKey = Guid.NewGuid().ToString(),
                    ImageUrl = blobClient.Uri.ToString(),
                    Name = name,
                    TemperatureProfile = temperatureProfile
                };

                // Save temperatureProfileEntity to Azure Table Storage
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("SnowProfiles");
                await table.CreateIfNotExistsAsync();

                TableOperation insertOperation = TableOperation.Insert(temperatureProfileEntity);
                await table.ExecuteAsync(insertOperation);


            }
        }

        private static async Task<BlobClient> UploadImage(MemoryStream uploadStream, string connectionString)
        {
            string containerName = "snowprofilepictures";
            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            string blobName = Guid.NewGuid().ToString();
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            BlobUploadOptions options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "image/jpeg", // Set the appropriate Content-Type for your image file
                }
            };

            await blobClient.UploadAsync(uploadStream, options);
            return blobClient;
        }

        public async Task<List<TemperatureProfileEntity>> GetAll()
        {
            string connectionString = _configuration["AzureStorageConnectionString"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("SnowProfiles");

            await table.CreateIfNotExistsAsync();

            var query = new TableQuery<TemperatureProfileEntity>();
            var tableQuerySegment = await table.ExecuteQuerySegmentedAsync(query, null);
            var results = tableQuerySegment.Results;
            return results;
        }

        public async Task<TemperatureProfileEntity?> Get(string id)
        {
            string connectionString = _configuration["AzureStorageConnectionString"];
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("SnowProfiles");

            await table.CreateIfNotExistsAsync();

            var partitionFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, id);
            var query = new TableQuery<TemperatureProfileEntity>().Where(partitionFilter);

            var tableQuerySegment = await table.ExecuteQuerySegmentedAsync(query, null);
            var result = tableQuerySegment.Results;
            var temperatureProfileEntity = result.FirstOrDefault(); // Assuming there's only one result for the given partitionKey
            return temperatureProfileEntity;
        }

    }
}
