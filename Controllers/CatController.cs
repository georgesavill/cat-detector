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
        private ImageProcessingService _imageProcessingService;

        public CatController(ILogger<CatController> logger, IConfiguration configuration, TelegramService telegramService, ImageProcessingService imageProcessingService)
        {
            _logger = logger;
            _configuration = configuration;
            _telegramService = telegramService;
            _imageProcessingService = imageProcessingService;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            _logger.LogInformation("Get recieved");
            //Load sample data
            (string imageLocation, string imageFilename) = await GetImageLocation();
            var sampleData = new MLModel.ModelInput()
            {
                ImageSource =  imageLocation,
            };
             
            string catStatus = await Task.FromResult(MLModel.Predict(sampleData).Prediction);

            _imageProcessingService.MoveImage(imageLocation, @"/media/" + catStatus + "/" + imageFilename);

            if (catStatus == "cat")
            {
                foreach (TelegramUserClass telegramUser in _configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>().TelegramUsers)
                {
                    _telegramService.SendMessage(telegramUser.Id, "CAT");
                }
            }
            _logger.LogInformation("Returning status: {0}", catStatus);
            return catStatus;
        }

        async Task<(string, string)> GetImageLocation()
        {
            _logger.LogInformation("GetImageLocation() called");
            string imageFilename = DateTimeOffset.Now.ToUnixTimeSeconds() + ".jpg";
            string imageLocation = "/media/" + imageFilename;
            HttpClient httpClient = new HttpClient();
            var byteArray = new UTF8Encoding().GetBytes(_configuration.GetSection("Config").GetSection("CctvAuth").Value.ToString());
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            using (var contentStream = await httpClient.GetStreamAsync(_configuration.GetSection("Config").GetSection("CctvUrl").Value.ToString()))
            using (var fileStream = new FileStream(imageLocation, FileMode.Create, FileAccess.Write, FileShare.None, 1048576, true))
            {
                await contentStream.CopyToAsync(fileStream);
            }

            _imageProcessingService.CropImage(imageLocation);
            
            return (imageLocation, imageFilename);
        }
    }
}