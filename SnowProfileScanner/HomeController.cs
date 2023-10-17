using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using SnowProfileScanner.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

public class HomeController : Controller
{

    private readonly IConfiguration _configuration;

    public HomeController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file, string name)
    {
        string endpoint = _configuration["AzureFormRecognizerEndpoint"];
        string key = _configuration["AzureFormRecognizerApiKey"];
        AzureKeyCredential credential = new AzureKeyCredential(key);
        DocumentAnalysisClient client = new DocumentAnalysisClient(new Uri(endpoint), credential);

        using (var memoryStream = new MemoryStream())
        {
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            AnalyzeDocumentOperation operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "KodedagModel", memoryStream);
            await operation.WaitForCompletionAsync();

            AnalyzeResult result = operation.Value;

            var temperatureProfile = new TemperatureProfile
            {
                SnowTemp = DecodeSnowTemperature(result.Tables[1]),
                Layers = DecodeSnowProfile(result.Tables[0]),
                AirTemp = DecodeAirTemperature(result.Tables[1])
            };

            using (var uploadStream = new MemoryStream(memoryStream.ToArray()))
            {
                // Upload image to Azure Blob Storage
                string connectionString = _configuration["AzureStorageConnectionString"];
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
         

            return View("Result", temperatureProfile);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ViewAllEntries()
    {
        string connectionString = _configuration["AzureStorageConnectionString"];
        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
        CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
        CloudTable table = tableClient.GetTableReference("SnowProfiles");

        await table.CreateIfNotExistsAsync();

        var query = new TableQuery<TemperatureProfileEntity>();
        var tableQuerySegment = await table.ExecuteQuerySegmentedAsync(query, null);
        var results = tableQuerySegment.Results;

        return View("ViewAllResults", results);
    }
    [HttpGet]
    public async Task<IActionResult> Result(string id)
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

        if (temperatureProfileEntity != null)
        {
            // Convert TemperatureProfileEntity to TemperatureProfile
            var temperatureProfile = new TemperatureProfile
            {
                // Populate properties based on temperatureProfileEntity properties
                AirTemp = temperatureProfileEntity.TemperatureProfile.AirTemp,
                Layers = temperatureProfileEntity.TemperatureProfile.Layers, // Assuming Layers is a collection in TemperatureProfileEntity
                SnowTemp = temperatureProfileEntity.TemperatureProfile.SnowTemp // Assuming SnowTemp is a collection in TemperatureProfileEntity
            };

            return View("Result", temperatureProfile);
        }
        else
        {
            // Handle the case where no matching entity is found
            return NotFound();
        }
    }


    private IEnumerable<TemperatureProfile.SnowTemperature> DecodeSnowTemperature(DocumentTable tbl1)
    {
        var snowTemps = tbl1.Cells
            .Where(cell => cell.RowIndex > 1 && !string.IsNullOrWhiteSpace(cell.Content))
            .GroupBy(cell => cell.RowIndex)
            .Select(group => new TemperatureProfile.SnowTemperature
            {
                Depth = group.FirstOrDefault()?.Content?.Replace(" ", string.Empty) ?? "",
                Temp = group.LastOrDefault()?.Content?.Replace(" ", string.Empty) ?? ""
            });

        return snowTemps.ToList();
    }

    private IEnumerable<TemperatureProfile.SnowProfile> DecodeSnowProfile(DocumentTable tbl1)
    {
        var snowProfiles = new List<TemperatureProfile.SnowProfile>();
        var rowsCount = tbl1.Cells
            .Where(cell => cell.ColumnIndex == 0 && !string.IsNullOrWhiteSpace(cell.Content))
            .Max(cell => cell.RowIndex);

        for (int rowIndex = 1; rowIndex <= rowsCount; rowIndex++)
        {
            var row = tbl1.Cells.Where(cell => cell.RowIndex == rowIndex);
            var lwc = row.SingleOrDefault(cell => cell.ColumnIndex == 1)?.Content?.Replace(" ", string.Empty) ?? "";
            if(lwc == "0")
            {
                lwc = "D";
            }
            var snowProfile = new TemperatureProfile.SnowProfile
            {
                Thickness = row.SingleOrDefault(cell => cell.ColumnIndex == 0)?.Content?.Replace(" ", string.Empty) ?? "",
                LWC = lwc,
                Hardness = row.SingleOrDefault(cell => cell.ColumnIndex == 2)?.Content?.Replace(" ", string.Empty) ?? "",
                Grain = row.SingleOrDefault(cell => cell.ColumnIndex == 3)?.Content?.Replace(" ", string.Empty) ?? "",
                Size = row.SingleOrDefault(cell => cell.ColumnIndex == 4)?.Content?.Replace(" ", string.Empty) ?? ""
            };

            snowProfiles.Add(snowProfile);
        }

        return snowProfiles;
    }


    private string DecodeAirTemperature(DocumentTable tbl1)
    {
        var airTemp = tbl1.Cells
            .SingleOrDefault(cell => cell.RowIndex == 1 && cell.ColumnIndex == 1)?
            .Content?
            .Replace(" ", string.Empty) ?? "";

        return airTemp;
    }

}
