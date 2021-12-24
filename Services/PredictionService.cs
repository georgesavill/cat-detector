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
        private DateTime _lastNotificationSent;
        private DateTime _lastNoneImageSaved;

        public PredictionService(ILogger<PredictionService> logger, IConfiguration configuration, TelegramService telegramService, ImageService imageService)
        {
            _logger = logger;
            _configuration = configuration;
            _telegramService = telegramService;
            _imageService = imageService;
        }

        public async Task<string> MakePrediction()
        {
            _logger.LogDebug("MakePrediction() called");

            (string imageLocation, string imageFilename) = await _imageService.DownloadCctvImage();

            _imageService.CropImage(imageLocation);

            MLModel.ModelInput imageData = new MLModel.ModelInput() { ImageSource = imageLocation };
            MLModel.ModelOutput prediction =  await Task.FromResult(MLModel.Predict(imageData));

            if (prediction.Prediction != "none")
            {
                _logger.LogInformation("PREDICTION: {0}", JsonSerializer.Serialize(prediction));
                _imageService.MoveImage(imageLocation, @"/media/" + prediction.Prediction + "/" + imageFilename);

                foreach (TelegramUserClass telegramUser in _configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>().TelegramUsers)
                {
                    if ((DateTime.Now - _lastNotificationSent).TotalMinutes >= _configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>().MinutesBetweenAlerts)
                    {
                        if (telegramUser.Admin)
                        {
                            _logger.LogDebug("Alerting admin user");
                            _telegramService.SendMessage(telegramUser.Id, JsonSerializer.Serialize(prediction));
                            _lastNotificationSent = DateTime.Now;
                        }
                        else if (prediction.Prediction == "cat")
                        {
                            _logger.LogDebug("Alerting non-admin user");
                            _telegramService.SendMessage(telegramUser.Id, "Mr Pussycat is waiting...");
                            _lastNotificationSent = DateTime.Now;
                        }
                    }
                }
            }
            else
            {
                if ((DateTime.Now - _lastNoneImageSaved).TotalMinutes >= _configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>().MinutesBetweenNoneImageSaved)
                {
                    _logger.LogDebug("Saving non-event image");
                    _imageService.MoveImage(imageLocation, @"/media/" + prediction.Prediction + "/" + imageFilename);
                    _lastNoneImageSaved = DateTime.Now;
                } 
                else
                {
                    _logger.LogDebug("Deleting non-event image");
                    File.Delete(imageLocation);
                }
            }

            return prediction.Prediction;
        }
    }
}
