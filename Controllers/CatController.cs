using Cat_detector;
using cat_detector.Classes;
using cat_detector.Services;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Text;

namespace cat_detector.Controllers
{
    [ApiController]
    [Route("/")]
    public class CatController : ControllerBase
    {
        private readonly ILogger<CatController> _logger;
        private IConfiguration _configuration;
        private TelegramService _telegramService;
        private ImageService _imageService;

        public CatController(ILogger<CatController> logger, IConfiguration configuration, TelegramService telegramService, ImageService imageService)
        {
            _logger = logger;
            _configuration = configuration;
            _telegramService = telegramService;
            _imageService = imageService;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            _logger.LogInformation("Get recieved");

            (string imageLocation, string imageFilename) = await _imageService.DownloadCctvImage();

            _imageService.CropImage(imageLocation);

            var sampleData = new MLModel.ModelInput() { ImageSource =  imageLocation };
             
            MLModel.ModelOutput catPrediction = await Task.FromResult(MLModel.Predict(sampleData));

            string catStatus = catPrediction.Prediction;
            string catScore = catPrediction.Score[0].ToString("P2");

            _imageService.MoveImage(imageLocation, @"/media/" + catStatus + "/" + imageFilename);

            if (catStatus == "cat")
            {
                foreach (TelegramUserClass telegramUser in _configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>().TelegramUsers)
                {
                    _telegramService.SendMessage(telegramUser.Id, catScore + " " + catStatus);
                }
            }
            _logger.LogInformation("Returning status: {0}", catStatus);
            return catStatus;
        }
    }
}