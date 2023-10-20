using Microsoft.AspNetCore.Mvc;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using SnowProfileScanner.Models;
using System.Globalization;
using SnowProfileScanner.Services;
using System.Text.Json;

public class UploadController : Controller
{

    private readonly IConfiguration _configuration;
    private readonly TemperatureProfileService _temperatureProfileService;
    private readonly HttpClient _httpClient;
    private const string RECAPTCHA_URL = "https://www.google.com/recaptcha/api/siteverify";
    private static readonly HashSet<string> VALID_LWC = ValidLwc();
    private static readonly HashSet<string> VALID_HARDNESS = ValidHardness();
    private static readonly HashSet<string> VALID_GRAINTYPE = ValidGrainType();

    private readonly string _remoteServiceBaseUrl;

    public UploadController(
        IConfiguration configuration,
        TemperatureProfileService temperatureProfileService,
        HttpClient httpClient
    ) {
        _httpClient = httpClient;
        _configuration = configuration;
        _temperatureProfileService = temperatureProfileService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [Microsoft.AspNetCore.Mvc.HttpPost]
    public async Task<IActionResult> Upload(
        IFormFile file,
        string name,
        [FromForm(Name="g-recaptcha-response")] string recaptchaResponse
    ) {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        string endpoint = _configuration["AzureFormRecognizerEndpoint"];
        string key = _configuration["AzureFormRecognizerApiKey"];
        AzureKeyCredential credential = new AzureKeyCredential(key);
        DocumentAnalysisClient client = new DocumentAnalysisClient(new Uri(endpoint), credential);

        if (bool.Parse(_configuration["UseCaptcha"]) && !await IsCaptchaValid(recaptchaResponse))
        {
            return this.StatusCode(401);
        }

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
            await _temperatureProfileService.UploadProfile(name, memoryStream, temperatureProfile);

            return View("Result", temperatureProfile);
        }
    }

    
    

    private IEnumerable<TemperatureProfile.SnowTemperature> DecodeSnowTemperature(DocumentTable tbl1)
    {
        var rows = tbl1.Cells
            .Where(cell => cell.RowIndex > 1 && !string.IsNullOrWhiteSpace(cell.Content))
            .GroupBy(cell => cell.RowIndex)
            .Select(
                group => group.ToList().Select(ToDouble)
            );
        double? previousDepth = null;
        var temps = new List<TemperatureProfile.SnowTemperature>();
        foreach (var row in rows)
        {
            var tempTuple = new TemperatureProfile.SnowTemperature() { };

            var d = row.ElementAtOrDefault(0);
            if (d >= 0 && (previousDepth is null || previousDepth < d))
            {
                previousDepth = d;
                tempTuple.Depth = d;
            }

            var t = row.ElementAtOrDefault(1);
            tempTuple.Temp = t <= 0 ? t : null;
            temps.Add(tempTuple);
        }
        return temps;
    }

    private static IEnumerable<TemperatureProfile.SnowProfile> DecodeSnowProfile(DocumentTable tbl1)
    {
        var snowProfiles = new List<TemperatureProfile.SnowProfile>();
        var rowsCount = tbl1.Cells
            .Where(cell => cell.ColumnIndex == 0 && !string.IsNullOrWhiteSpace(cell.Content))
            .Max(cell => cell.RowIndex);

        for (int rowIndex = 1; rowIndex <= rowsCount; rowIndex++)
        {
            var row = tbl1.Cells.Where(cell => cell.RowIndex == rowIndex);

            var thickness = ToDouble(row.SingleOrDefault(cell => cell.ColumnIndex == 0));

            var lwc = ToString(row.SingleOrDefault(cell => cell.ColumnIndex == 1));
            if (new List<string>() { "0", "P" }.Contains(lwc))
            {
                lwc = "D";
            }

            var hardness = ToString(row.SingleOrDefault(cell => cell.ColumnIndex == 2))
                .Replace("IF", "1F");

            var grainType = ToString(row.SingleOrDefault(cell => cell.ColumnIndex == 3))
                .Replace("1F", "IF");

            var grainSize = ToDouble(row.SingleOrDefault(cell => cell.ColumnIndex == 4));

            var snowProfile = new TemperatureProfile.SnowProfile
            {
                Thickness = thickness > 0 ? thickness : null,
                LWC = VALID_LWC.Contains(lwc) ? lwc : null,
                Hardness = VALID_HARDNESS.Contains(hardness) ? hardness : null,
                Grain = VALID_GRAINTYPE.Contains(grainType) ? grainType : null,
                Size = grainSize > 0 && grainSize < 40 ? grainSize : null
            };

            snowProfiles.Add(snowProfile);
        }

        return snowProfiles;
    }


    private static double? DecodeAirTemperature(DocumentTable tbl1)
    {
        return ToDouble(tbl1.Cells.SingleOrDefault(
            cell => cell.RowIndex == 1 && cell.ColumnIndex == 1
        ));
    }

    private static HashSet<string> ValidLwc()
    {
        return new HashSet<string>() {
            "D", "D-M", "M", "M-W", "W", "W-V", "V", "V-S", "S"
        };
    }

    private static HashSet<string> ValidHardness()
    {
        var hardnesses = new List<string>();
        var baseHardnesses = new List<string>() { "F", "4F", "1F", "P", "K", "I" };
        for (var i = 0; i < baseHardnesses.Count; i++)
        {
            var hardness = baseHardnesses[i];
            hardnesses.Add(hardness + "-");
            hardnesses.Add(hardness);
            hardnesses.Add(hardness + "+");
            if (i < baseHardnesses.Count - 1) hardnesses.Add(hardness + "-" + baseHardnesses[i + 1]);
        }

        var hardnessGradients = new List<string>();
        foreach(var upperHardness in hardnesses)
        {
            foreach(var lowerHardness in hardnesses)
            {
                if (upperHardness == lowerHardness) continue;
                hardnessGradients.Add(upperHardness + "/" + lowerHardness);
            }
        }

        return hardnesses.Concat(hardnessGradients).ToHashSet();
    }

    private static HashSet<string> ValidGrainType()
    {
        var types = new HashSet<string>();
        var baseTypes = new List<string>()
        {
            "PP", "PPco", "PPnd", "PPpl", "PPsd", "PPir", "PPgp", "PPhl", "PPip", "PPrm",
            "MM", "MMrp", "MMci",
            "DF", "DFdc", "DFbk",
            "RG", "RGsr", "RGlr", "RGwp", "RGxf",
            "FC", "FCso", "FCsf", "FCxr",
            "DH", "DHcp", "DHpr", "DHch", "DHla", "DHxr",
            "SH", "SHsu", "SHcv", "SHxr",
            "MF", "MFcl", "MFpc", "MFsl", "MFcr",
            "IF", "IFil", "IFic", "IFbi", "IFrc", "IFsc",
        };
        foreach(var type in baseTypes)
        {
            types.Add(type);
            foreach(var secType in baseTypes) {
                if (type == secType) continue;
                types.Add(type + "(" + secType + ")");
            }
        }
        return types;
    }

    private static double? ToDouble(DocumentTableCell? cell)
    {
        var sReplaced = cell?.Content?
            .Replace(" ", string.Empty)
            .Replace(",", ".")
            .Replace("Overflate", "0")
            .Replace("I", "1")
            .Replace("l", "1");
        return double.TryParse(sReplaced, out var d) ? d : null;
    }

    private static string ToString(DocumentTableCell? cell)
    {
        return cell?.Content?.Replace(" ", string.Empty) ?? "";
    }

    private async Task<bool> IsCaptchaValid(string recaptchaResponse)
    {
        var recaptchaBody = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "secret", _configuration["RecaptchaSecret"] },
                { "response", recaptchaResponse }
            });
        var recaptchaValidationResponse = await _httpClient.PostAsync(RECAPTCHA_URL, recaptchaBody);
        var result = JsonSerializer.Deserialize<RecaptchaValidationResponse>(
            await recaptchaValidationResponse.Content.ReadAsStringAsync()
        )?.success ?? false;
        return result;
    }
}
