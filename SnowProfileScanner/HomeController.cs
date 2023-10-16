using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using SnowProfileScanner.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;


namespace SnowProfileScanner.Models
{
    public class TemperatureProfile
    {
        public IEnumerable<SnowProfile> Layers { get; set; }
        public string? AirTemp { get; set; }
        public IEnumerable<SnowTemperature> SnowTemp { get; set; }

        public TemperatureProfile()
        {
            SnowTemp = new List<SnowTemperature>();
            Layers = new List<SnowProfile>();
        }

        public class SnowTemperature
        {
            public string Depth { get; set; }
            public string Temp { get; set; }
        }

        public class SnowProfile
        {
            public string Thickness { get; set; }
            public string Hardness { get; set; }
            public string Grain { get; set; }
            public string? Size { get; set; }
            public string LWC { get; set; }
        }
    }
}

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
    public async Task<IActionResult> Upload(IFormFile file)
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

            return View("Result", temperatureProfile);
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
