using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Newtonsoft.Json;
using SnowProfileScanner.Models;
using System.Net.Http.Headers;

namespace SnowProfileScanner.Services
{
    public class SymbolRecognitionService
    {
        private string url, predictionKey, projectId;
        private ILogger logger;
        private CustomVisionPredictionClient predictionClient;

        public SymbolRecognitionService(IConfiguration configuration, ILogger<SymbolRecognitionService> logger)
        {
            url = configuration["CustomVisionUrl"];
            predictionKey = configuration["CustomVisionPredictionKey"];
            projectId = configuration["CustomVisionProjectId"];

            this.logger = logger;
            predictionClient = AuthenticatePrediction(url, predictionKey);
        }

        public async Task<string> ClassifyImage(MemoryStream image)
        {
            var value = await predictionClient.ClassifyImageAsync(Guid.Parse(projectId), "Iteration2", image);
            if (value.Predictions[0].Probability > 0.30)
            {
                if(value.Predictions[0].TagName == "MDcr")
                {
                    return "MFcr";
                }
                return value.Predictions[0].TagName;
            }
            else
            {
                return null;
            }
            
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
