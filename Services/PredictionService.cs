using Cat_detector;
using cat_detector.Classes;
using System.Text.Json;

namespace cat_detector.Services
{
    public class PredictionService
    {
        private readonly ILogger<PredictionService> _logger;
        private IConfiguration _configuration;
        private TelegramService _telegramService;
        private ImageService _imageService;

        public PredictionService(ILogger<PredictionService> logger, IConfiguration configuration, TelegramService telegramService, ImageService imageService)
        {
            _logger = logger;
            _configuration = configuration;
            _telegramService = telegramService;
            _imageService = imageService;
        }

        public async Task<string> MakePrediction()
        {
            _logger.LogInformation("Get recieved");

            (string imageLocation, string imageFilename) = await _imageService.DownloadCctvImage();

            _imageService.CropImage(imageLocation);

            MLModel.ModelInput imageData = new MLModel.ModelInput() { ImageSource = imageLocation };
            MLModel.ModelOutput prediction =  await Task.FromResult(MLModel.Predict(imageData));

            if (prediction.Prediction != "none")
            {
                _imageService.MoveImage(imageLocation, @"/media/" + prediction + "/" + imageFilename);

                foreach (TelegramUserClass telegramUser in _configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>().TelegramUsers)
                {
                    if (telegramUser.Admin)
                    {
                        _telegramService.SendMessage(telegramUser.Id, JsonSerializer.Serialize(prediction));
                    }
                    else if (prediction.Prediction == "cat")
                    {
                        _telegramService.SendMessage(telegramUser.Id, "Mr Pussycat is waiting...");
                    }
                }
            }

            return prediction.Prediction;
        }
    }
}
