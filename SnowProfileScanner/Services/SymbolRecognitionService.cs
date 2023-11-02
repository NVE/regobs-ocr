using Newtonsoft.Json;
using SnowProfileScanner.Models;

namespace SnowProfileScanner.Services
{
    public class SymbolRecognitionService
    {
        private string url, predictionKey;
        private ILogger logger;

        public SymbolRecognitionService(IConfiguration configuration, ILogger<SymbolRecognitionService> logger)
        {
            url = configuration["CustomVisionUrl"];
            predictionKey = configuration["CustomVisionPredictionKey"];
            this.logger = logger;
        }

        public async Task<string> ClassifyImage(byte[] image)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Content-Type", "application/octet-stream");
            client.DefaultRequestHeaders.Add("Prediction-Key", predictionKey);
            var body = new ByteArrayContent(image);
            HttpResponseMessage response = await client.PostAsync(url, body);
            if (response.StatusCode.Equals(200))
            {
                var result = JsonConvert.DeserializeObject <ICustomVistionDecodeResult> (response.Content.ToString());
                logger.LogDebug("ClassifyImage: Fikk OK" + response.StatusCode, result);
                if (result.predictions.Length > 0) {
                    var prediction = result.predictions[0];
                    return prediction.TagName;
                }
                return ":-|";
            } else
            {
                logger.LogError("ClassifyImage: Fikk HTTP-respons-kode: " + response.StatusCode);
                return ":-(";
            }
        }
    }
}
