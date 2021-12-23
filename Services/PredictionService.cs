using Cat_detector;
using cat_detector.Classes;

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
            MLModel.ModelOutput catPrediction =  await Task.FromResult(MLModel.Predict(imageData));

            string catStatus = catPrediction.Prediction;
            //string catScore = catPrediction.Score[0].ToString("P2");

            _imageService.MoveImage(imageLocation, @"/media/" + catStatus + "/" + imageFilename);

            if (catStatus != "no-cat")
            {
                string message = catStatus + "%0AProb 1: " + catPrediction.Score[0].ToString("P2") + "%0AProb 2: " + catPrediction.Score[1].ToString("P2") + "%0AProb 3: " + catPrediction.Score[2].ToString("P2");
                _telegramService.SendMessage(_configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>().TelegramUsers[0].Id, message);

                //foreach (TelegramUserClass telegramUser in _configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>().TelegramUsers)
                //{
                //    _telegramService.SendMessage(telegramUser.Id, catScore + " " + catStatus);
                //}
            }

            return catStatus;
        }
    }
}
