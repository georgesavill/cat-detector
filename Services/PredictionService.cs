using Cat_detector;
using cat_detector.Classes;
using System.Text.Json;

namespace cat_detector.Services
{
    public class PredictionService
    {
        private readonly ILogger<PredictionService> _logger;
        private ConfigurationOptions _configurationOptions;
        private TelegramService _telegramService;
        private ImageService _imageService;
        private DateTime _lastNotificationSent;
        private DateTime _lastNoneImageSaved;
        private Queue<string> _predictionHistory = new Queue<string>(5);

        public PredictionService(ILogger<PredictionService> logger, IConfiguration configuration, TelegramService telegramService, ImageService imageService)
        {
            _logger = logger;
            _configurationOptions = configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>();
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

            _logger.LogDebug("Adding prediction to queue.");
            _predictionHistory.Enqueue(prediction.Prediction);
            foreach (string entry in _predictionHistory)
            {
                _logger.LogDebug(entry);
            }

            while (_predictionHistory.Count > _configurationOptions.ConsecutivePredictionThreshold)
            {
                _logger.LogDebug("Removing prediction from queue.");
                _logger.LogDebug("Prediction count: {0} and threshold: {1}", _predictionHistory.Count, _configurationOptions.ConsecutivePredictionThreshold);
                _predictionHistory.Dequeue();
            }
            foreach (string entry in _predictionHistory)
            {
                _logger.LogDebug(entry);
            }


            if (prediction.Prediction != "none")
            {
                _logger.LogInformation("PREDICTION: {0}", JsonSerializer.Serialize(prediction));
                _imageService.MoveImage(imageLocation, @"/media/" + prediction.Prediction + "/" + imageFilename);

                if (ConsecutivePredictionThresholdReached(prediction.Prediction))
                {
                    if ((DateTime.Now - _lastNotificationSent).TotalMinutes >= _configurationOptions.MinutesBetweenAlerts)
                    {
                        foreach (TelegramUserClass telegramUser in _configurationOptions.TelegramUsers)
                        {
                            if (telegramUser.Admin)
                            {
                                _logger.LogInformation("Alerting admin user");
                                _telegramService.SendMessage(telegramUser.Id, JsonSerializer.Serialize(prediction));
                                _telegramService.SendMessage(telegramUser.Id, JsonSerializer.Serialize(_predictionHistory));
                            }
                            if (prediction.Prediction == "cat" && prediction.Score[0] >= _configurationOptions.PredictionThreshold)
                            {
                                _logger.LogInformation("Alerting non-admin users");
                                _logger.LogDebug("Prediction score: {0} and threshold: {1}", prediction.Score[0], _configurationOptions.PredictionThreshold);
                                _telegramService.SendMessage(telegramUser.Id, "Mr Pussycat is waiting...");
                            }
                        }
                        _lastNotificationSent = DateTime.Now;
                    }
                }
            }
            else
            {
                if ((DateTime.Now - _lastNoneImageSaved).TotalMinutes >= _configurationOptions.MinutesBetweenNoneImageSaved)
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
