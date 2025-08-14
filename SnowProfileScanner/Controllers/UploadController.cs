using Microsoft.AspNetCore.Mvc;
using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using SnowProfileScanner.Models;
using System.Globalization;
using SnowProfileScanner.Services;
using System.Text.Json;
using System.Data;
using SnowProfileScanner.Services.Caaml;
using System.Text;
using System.Net;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats;
using System.IO;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.Advanced;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.RegularExpressions;

public class UploadController : Controller
{

    private readonly IConfiguration _configuration;
    private readonly SnowProfileService _snowProfileService;
    private readonly SymbolRecognitionService _symbolRecognitionService;
    private readonly HttpClient _httpClient;
    private const string RECAPTCHA_URL = "https://www.google.com/recaptcha/api/siteverify";
    private static readonly HashSet<string> VALID_LWC = ValidLwc();
    private static readonly HashSet<string> VALID_HARDNESS = ValidHardness();
    private static readonly HashSet<string> VALID_GRAINTYPE = ValidGrainType();

    private readonly string _remoteServiceBaseUrl;

    public UploadController(
        IConfiguration configuration,
        SnowProfileService snowProfileService,
        HttpClient httpClient,
        SymbolRecognitionService symbolService
    ) {
        _httpClient = httpClient;
        _configuration = configuration;
        _snowProfileService = snowProfileService;
        _symbolRecognitionService = symbolService;
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

        using var memoryStream = new MemoryStream();
        using var plotStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var options = new AnalyzeDocumentOptions() { Locale = "de" };
        AnalyzeDocumentOperation operation = await client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", memoryStream, options);
        await operation.WaitForCompletionAsync();

        AnalyzeResult result = operation.Value;

        var numberOfTables = result.Tables.Count();

        Image<Rgba32> image = Image.Load<Rgba32>(memoryStream.ToArray());
        image.Mutate(x => x.AutoOrient());

        var snowProfile = new SnowProfile
        {
            SnowTemp = numberOfTables > 1 ? DecodeSnowTemperature(result.Tables[1]) : new(),
            Layers = numberOfTables > 0 ? await DecodeSnowProfile(image, result) : new(),
            AirTemp = numberOfTables > 1 ? DecodeAirTemperature(result.Tables[1]) : null,
        };

        var plotStatus = await GetPlot(CaamlService.ConvertToCaaml(snowProfile), plotStream);
        var plot = plotStatus == HttpStatusCode.OK ? plotStream : null;
        var snowProfileEntity = await _snowProfileService.UploadProfile(name, memoryStream, plot, snowProfile);

        return View("Result", snowProfileEntity);
    }


	private Image<Rgba32> CropImageToBoundingBox(Image<Rgba32> image, IReadOnlyList<System.Drawing.PointF> boundingBox)
	{
		image.Mutate(x => x.AutoOrient());
		var rectangle = ConvertBoundingBoxToRectangle(boundingBox);
		return image.Clone(x => x.Crop(rectangle));
	}


	private Rectangle ConvertBoundingBoxToRectangle(IReadOnlyList<System.Drawing.PointF> boundingBox)
	{

		var topLeft = new Point((int)boundingBox.Select(p => p.X).Min(), (int)boundingBox.Select(p => p.Y).Min());
		var bottomRight = new Point((int)boundingBox.Select(p => p.X).Max(), (int)boundingBox.Select(p => p.Y).Max());
		var width = bottomRight.X - topLeft.X;
		var height = bottomRight.Y - topLeft.Y;
		return new Rectangle(topLeft.X, topLeft.Y, width, height);
	}



	private List<SnowProfile.SnowTemperature> DecodeSnowTemperature(DocumentTable tbl1)
    {
        var rows = tbl1.Cells
            .Where(cell => cell.RowIndex > 1 && !string.IsNullOrWhiteSpace(cell.Content))
            .GroupBy(cell => cell.RowIndex)
            .Select(
                group => group.ToList().Select(cell => ToDouble(
                    cell.Content
                        // Strip "T" since some observers prefixes temperature depths with T
                        .Replace("T", String.Empty)
                ))
            );
        double? previousDepth = null;
        var temps = new List<SnowProfile.SnowTemperature>();
        foreach (var row in rows)
        {
            var tempTuple = new SnowProfile.SnowTemperature() { };

            var d = row.ElementAtOrDefault(0);
            d = d is not null ? Math.Abs(d.Value) : null;
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

    private class ParsedText
    {
        public IEnumerable<BoundingRegion> BoundingRegions { get; set; }
        public string Content { get; set; }
    }

    private async Task<List<SnowProfile.Layer>> DecodeSnowProfile(Image<Rgba32> image, AnalyzeResult result)
    {
        var snowProfiles = new List<SnowProfile.Layer>();

        var tbl1 = result.Tables[0];
        int col1 = -1;
        foreach (var cell in tbl1.Cells)
        {
            if (cell.Content.ToLower().Contains("schneehöhe"))
            {
                col1 = cell.ColumnIndex + 1;
                break;
            }
            if (cell.Content.ToLower().Contains("feuchte"))
            {
                col1 = cell.ColumnIndex;
                break;
            }
            if (cell.Content.ToLower().Contains("kornform"))
            {
                col1 = cell.ColumnIndex - 1;
                break;
            }
            if (cell.Content.ToLower().Contains("härte"))
            {
                col1 = cell.ColumnIndex - 2;
                break;
            }
        }
        if (col1 == -1) return snowProfiles;


        IEnumerable<ParsedText> depthCells;
        if (col1 >= 1)
        {
            depthCells = tbl1.Cells
                .Where(cell => cell.ColumnIndex == col1 - 1)
                .Select(cell => new ParsedText() { BoundingRegions = cell.BoundingRegions, Content = cell.Content });
        } else
        {
            var depthTitle = result.Paragraphs.Where(p => p.Content.ToLower().Contains("schneehöhe")).FirstOrDefault();
            if (depthTitle == null) return snowProfiles;

            var depthTitleXmin = depthTitle.BoundingRegions.First().BoundingPolygon.Select(r => r.X).Min();
            var depthTitleXmax = depthTitle.BoundingRegions.First().BoundingPolygon.Select(r => r.X).Max();
            depthCells = result.Paragraphs.Where(p =>
            {
                var depthPX = p.BoundingRegions.First().BoundingPolygon.Select(r => r.X).Average();
                return depthTitleXmin <= depthPX && depthTitleXmax > depthPX;
            })
            .Select(p => new ParsedText() { BoundingRegions = p.BoundingRegions, Content = p.Content });
        }

        var rowsCount = tbl1.Cells
            .Where(cell => !string.IsNullOrWhiteSpace(cell.Content))
            .Max(cell => cell.RowIndex);

        double? previousDepth = null;
        for (int rowIndex = 0; rowIndex <= rowsCount; rowIndex++)
        {
            var grainSizeCell = tbl1.Cells
                .Where(cell => cell.RowIndex == rowIndex && cell.ColumnIndex == col1 + 2 && !String.IsNullOrEmpty(cell.Content))
                .FirstOrDefault();
            if (grainSizeCell is null) continue;
            var grainSizeCellYs = grainSizeCell.BoundingRegions.First().BoundingPolygon.Select(r => r.Y);
            var row = tbl1.Cells.Where(cell =>
            {
                var cellYs = cell.BoundingRegions.First().BoundingPolygon.Select(r => r.Y);
                var cellYmin = cellYs.Min();
                var cellYmax = cellYs.Max();
                return grainSizeCellYs.Average() >= cellYmin && grainSizeCellYs.Average() < cellYmax;
            });

            double? leftMostCellYmin = null, leftMostCellYmax = null;
            for (int i = 0; i < 3; i++)
            {
                var leftMostCol = row.Where(c => c.ColumnIndex == col1);
                if (leftMostCol.Count() == 0) continue;
                leftMostCellYmin = leftMostCol.Select(c => c.BoundingRegions.First().BoundingPolygon[0].Y).Min();
                leftMostCellYmax = leftMostCol.Select(c => c.BoundingRegions.First().BoundingPolygon[3].Y).Max();
                break;
            }

            var depths = depthCells
                .Where(cell =>
                {
                    var depthYs = cell.BoundingRegions.First().BoundingPolygon.Select(r => r.Y).ToArray();
                    var depthYmin = depthYs[1];
                    var depthYmax = depthYs[2];
                    return leftMostCellYmin >= depthYmin && leftMostCellYmin <= depthYmax ||
                           leftMostCellYmax >= depthYmin && leftMostCellYmax <= depthYmax;
                })
                .Select(cell => ToDouble(
                    Regex.Replace(cell.Content, "[^0-9,.]", "")
                        .Replace('.', ',')
                ));
            var thickness = depths.Max() - depths.Min();
            if (thickness == 0 || depths.Max() >= previousDepth)
            {
                thickness = null;
            } else
            {
                previousDepth = depths.Max();
            }

            var grainSizeText = Regex.Replace(grainSizeCell.Content ?? "", "[^0-9-–—_,.]", "")
                .Replace('.', ',');
            var grainSizeSplit = grainSizeText.Split(new Char[] { '-', '–', '—', '_' });
            var grainSize = ToDouble(grainSizeSplit.First());
            double? grainSizeMax = null;
            if (grainSize is not null && grainSizeSplit.Length > 1)
            {
                grainSizeMax = ToDouble(grainSizeSplit.Last());
            }

            var lwc = row
                .Where(cell => cell.ColumnIndex == col1)
                .Select(cell => Regex.Replace(ToString(cell), "[^0-9-]", "")
                    .Replace("1", "D")
                    .Replace("2", "M")
                    .Replace("3", "W")
                    .Replace("4", "V")
                    .Replace("5", "S"))
                .Select(lwc => VALID_LWC
                    .Where(vlwc => vlwc.Length <= lwc.Length && vlwc == lwc[..vlwc.Length])
                    .OrderBy(vlwc => -vlwc.Length)
                    .FirstOrDefault())
                .Where(lwc => !String.IsNullOrEmpty(lwc))
                .FirstOrDefault();

            var hardness = row
                .Where(cell => cell.ColumnIndex == col1 + 3)
                .Select(cell => Regex.Replace(ToString(cell), "[^0-9-+]", "")
                    .ToUpper()
                    .Replace("1", "F")
                    .Replace("3", "1F")
                    .Replace("4", "P")
                    .Replace("2", "4F")
                    .Replace("5", "K")
                    .Replace("6", "I")
                    .Replace(" ", string.Empty) ?? string.Empty)
                .Select(hardness => VALID_HARDNESS
                    .Where(vh => vh.Length <= hardness.Length && vh == hardness[..vh.Length])
                    .OrderBy(vh => -vh.Length)
                    .FirstOrDefault())
                .Where(hardness => !String.IsNullOrEmpty(hardness))
                .FirstOrDefault();

            var grainCells = row.Where(cell => cell.ColumnIndex == col1 + 1);
            var grainTypeText = grainCells
                .Select(cell => ToString(cell)
                    .Replace("t", "f")
                    .Replace("I", "l")
                    .Replace("1", "l")
                    .Replace("|", "l")
                    .ToUpper()
                    .Replace("LF", "IF")
                    .Replace("KR", "XR")
                    .Replace("KF", "XF")
                    .Replace("\\", "/")
                    .Replace(" ", string.Empty) ?? string.Empty)
                .Select(grainTypeText => VALID_GRAINTYPE
                    .Where(vg => vg.Length <= grainTypeText.Length && vg.ToUpper() == grainTypeText[..vg.Length])
                    .OrderBy(vg => -vg.Length)
                    .FirstOrDefault())
                .Where(hardness => !String.IsNullOrEmpty(hardness))
                .FirstOrDefault();
            if (grainCells.Count() >= 0 && (grainTypeText == null || grainTypeText == string.Empty))
            {
                foreach (var grainCell in grainCells)
                {
                    var polygon = grainCell.BoundingRegions[0].BoundingPolygon;
                    var croppedImage = CropImageToBoundingBox(image, polygon);
                    using var stream = new MemoryStream();
                    croppedImage.SaveAsPng(stream); // Save the image to the stream in PNG format
                    stream.Position = 0;

                    grainTypeText = await _symbolRecognitionService.ClassifyImage(stream);
                    if (!String.IsNullOrEmpty(grainTypeText)) break;
                }
            }
                
            var grainType = grainTypeText?.GetPrimaryGrainForm();
            if (grainType?.Count() == 4)
            {
                grainType = grainType.Substring(0, 2) + grainType.Substring(2).ToLower();
            }
            //string grainTypeSec = null;
            var grainTypeSec = grainTypeText?.GetSecondaryGrainForm();
            if (grainTypeSec is not null && grainTypeSec.Count() == 4)
            {
                grainTypeSec = grainTypeSec.Substring(0, 2) + grainTypeSec.Substring(2).ToLower();
            }
            
            
            if (lwc is not null || grainSize is not null || hardness is not null)
            {
                var snowProfile = new SnowProfile.Layer
                {
                    Thickness = thickness > 0 ? thickness : null,
                    LWC = lwc,
                    Hardness = hardness,
                    Grain = grainType,
                    GrainSecondary = grainTypeSec,
                    Size = grainSize > 0 && grainSize < 40 ? grainSize : null,
                    SizeMax = grainSizeMax > 0 && grainSizeMax < 40 && grainSizeMax > grainSize
                        ? grainSizeMax : null,
                };

                snowProfiles.Add(snowProfile);
            }
        }

        return snowProfiles;
    }


    private static double? DecodeAirTemperature(DocumentTable tbl1)
    {
        return ToDouble(tbl1.Cells.SingleOrDefault(
            cell => cell.RowIndex == 1 && cell.ColumnIndex == 1
        )?.Content);
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
        for (var i = 0; i < baseHardnesses.Count(); i++)
        {
            var hardness = baseHardnesses[i];
            hardnesses.Add(hardness + "-");
            hardnesses.Add(hardness);
            hardnesses.Add(hardness + "+");
            if (i < baseHardnesses.Count() - 1) hardnesses.Add(hardness + "-" + baseHardnesses[i + 1]);
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

    private static double? ToDouble(string? s)
    {
        var sReplaced = s?
            .Replace("Overflate", "0")
            .Replace("l", "1")
            .ToUpper()
            .Replace("|", "1")
            .Replace("I", "1")
            .Replace(" ", string.Empty)
            .Replace(",", ".")
            .Replace("–", "-")
            .Replace("—", "-")
            .Replace("_", "-")
            .Replace("O", "0")
            .Replace("Z", "2")
            .Replace("G", "6");
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

    private async Task<HttpStatusCode> GetPlot(string caaml, MemoryStream ms)
    {
        var plotUrl = _configuration["PlotUrl"];
        var body = new StringContent(caaml, Encoding.UTF8, "text/xml"); ;
        var plotResponse = await _httpClient.PostAsync(plotUrl, body);
        plotResponse.Content.ReadAsStream().CopyTo(ms);
        return plotResponse.StatusCode;
    }

    [Serializable]
    public class RemoteResourceException : Exception { }
}
