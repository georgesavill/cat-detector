using Cat_detector;
using cat_detector.Classes;
using System.Text.Json;

namespace cat_detector.Services
{
    public class PredictionService
    {
        private readonly ILogger<PredictionService> _logger;
        private ConfigurationOptions _configurationOptions;
        private NotificationService _notificationService;
        private ImageService _imageService;
        private DateTime _lastNotificationSent;
        private DateTime _lastNoneImageSaved;
        private Queue<string> _predictionHistory = new Queue<string>(5);

        public PredictionService(ILogger<PredictionService> logger, IConfiguration configuration, NotificationService notificationService, ImageService imageService)
        {
            _logger = logger;
            _configurationOptions = configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>();
            _notificationService = notificationService;
            _imageService = imageService;
        }

        public async Task<string> MakePrediction()
        {
            _logger.LogDebug("MakePrediction() called");

            (string imageLocation, string imageFilename) = await _imageService.DownloadCctvImage();

            _imageService.CropImage(imageLocation);

            MLModel.ModelInput imageData = new MLModel.ModelInput() { ImageSource = imageLocation };
            MLModel.ModelOutput prediction =  await Task.FromResult(MLModel.Predict(imageData));

            _predictionHistory.Enqueue(prediction.Prediction);

            while (_predictionHistory.Count > _configurationOptions.ConsecutivePredictionThreshold)
            {
                _logger.LogDebug("Removing prediction from queue.");
                _predictionHistory.Dequeue();
            }

            if (prediction.Prediction != "none")
            {
                _logger.LogInformation("PREDICTION: {0}", JsonSerializer.Serialize(prediction));

                if (prediction.Score[3] < (1 - _configurationOptions.PredictionThreshold) && prediction.Prediction == "human")
                {
                    _imageService.MoveImage(imageLocation, @"/media/" + prediction.Prediction + "/" + imageFilename);
                }
                else
                {
                    File.Delete(imageLocation);
                }

                if (ConsecutivePredictionThresholdReached(prediction.Prediction))
                {
                    if ((DateTime.Now - _lastNotificationSent).TotalMinutes >= _configurationOptions.MinutesBetweenAlerts)
                    {
                        foreach (TelegramUserClass telegramUser in _configurationOptions.TelegramUsers)
                        {
                            if (prediction.Prediction == "cat" && prediction.Score[1] >= _configurationOptions.PredictionThreshold)
                            {
                                _logger.LogDebug("Prediction score: {0} and threshold: {1}", prediction.Score[1], _configurationOptions.PredictionThreshold);
                                _notificationService.SendTelegramMessage(telegramUser.Id, "Mr Pussycat is waiting...");
                            }
                            else if (prediction.Prediction == "human" && prediction.Score[2] >= _configurationOptions.PredictionThreshold && telegramUser.Admin)
                            {
                                _logger.LogDebug("Prediction score: {0} and threshold: {1}", prediction.Score[2], _configurationOptions.PredictionThreshold);
                                _notificationService.SendTelegramMessage(telegramUser.Id, "There is a person at the door");
                            }
                        }
                        if (prediction.Prediction == "cat")
                        {
                            _logger.LogDebug("Triggering cat webhook");
                            _notificationService.TriggerWebhook(_configurationOptions.WebhookUrlCat);
                        } 
                        else if (prediction.Prediction == "human")
                        {
                            _logger.LogDebug("Triggering human webhook");
                            _notificationService.TriggerWebhook(_configurationOptions.WebhookUrlHuman);
                        }
                        _lastNotificationSent = DateTime.Now;
                    }
                }
            }
            else
            {
                _logger.LogDebug("Deleting non-event image");
                File.Delete(imageLocation);
            }

            return prediction.Prediction;
        }
        private bool ConsecutivePredictionThresholdReached(string prediction)
        {
            _logger.LogDebug("ConsecutivePredictionThresholdReached() called");
            bool predictionHistoryConsistent = true;
            foreach(string historicalPrediction in _predictionHistory)
            {
                if (historicalPrediction != prediction)
                {
                    predictionHistoryConsistent = false;
                }
            }
            _logger.LogDebug("Returning " + predictionHistoryConsistent);
            return predictionHistoryConsistent;
        }
    }
}
