using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Newtonsoft.Json;
using SnowProfileScanner.Models;
using System.Net.Http.Headers;

namespace SnowProfileScanner.Services
{
    public class SymbolRecognitionService
    {
        private string url, predictionKey;
        private ILogger logger;
        private CustomVisionPredictionClient predictionClient;

        public SymbolRecognitionService(IConfiguration configuration, ILogger<SymbolRecognitionService> logger)
        {
            url = configuration["CustomVisionUrl"];
            predictionKey = configuration["CustomVisionPredictionKey"];
            this.logger = logger;
            predictionClient = AuthenticatePrediction(url, predictionKey);
        }

        public async Task<string> ClassifyImage(Stream image)
        {
            var value = await predictionClient.DetectImageAsync(new Guid("e05d2a8c-6fd0-4c44-820b-e9aa43785aae"), "Regobs-Image-Recognition", image);
            return value.Predictions[0].TagName;
        }
        private static CustomVisionPredictionClient AuthenticatePrediction(string endpoint, string predictionKey)
        {
            // Create a prediction endpoint, passing in the obtained prediction key
            CustomVisionPredictionClient predictionApi = new CustomVisionPredictionClient(new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.ApiKeyServiceClientCredentials(predictionKey))
            {
                Endpoint = endpoint
            };
            return predictionApi;
        }
    }
}
