using Cat_detector;
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

        public CatController(ILogger<CatController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
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

            MoveImage(catStatus, imageLocation, imageFilename);

            if (catStatus == "cat")
            {
                SendNotification();
            }
            _logger.LogInformation("Returning status: " + catStatus);
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

            CropImage(imageLocation);

            return (imageLocation, imageFilename);
        }

        void MoveImage(string imageCategory, string imageLocation, string imageFilename)
        {
            _logger.LogInformation("MoveImage() called");
            string newImageLocation = @"/media/" + imageCategory + "/" + imageFilename;
            try
            {
                if (!System.IO.File.Exists(imageLocation))
                {
                    // This statement ensures that the file is created,
                    // but the handle is not kept.
                    using (FileStream fs = System.IO.File.Create(imageLocation)) { }
                }

                // Ensure that the target does not exist.
                if (System.IO.File.Exists(newImageLocation))
                    System.IO.File.Delete(newImageLocation);

                // Move the file.
                System.IO.File.Move(imageLocation, newImageLocation);
                _logger.LogInformation("{0} was moved to {1}.", imageLocation, newImageLocation);

                // See if the original exists now.
                if (System.IO.File.Exists(imageLocation))
                {
                    _logger.LogError("The original file still exists, which is unexpected.");
                }
                else
                {
                    _logger.LogInformation("The original file no longer exists, which is expected.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError("The process failed: {0}", e.ToString());
            }
        }

        void CropImage(string imageLocation)
        {
            _logger.LogInformation("CropImage() called");

            using (Image image = Image.Load(imageLocation))
            {
                image.Mutate(x => x.Crop(new Rectangle(0, (image.Height - 500), 500, 500)));
                image.Save(imageLocation);
            }
        }

        async void SendNotification()
        {
            _logger.LogInformation("SendNotification() called");

            HttpClient client = new HttpClient();
            HttpResponseMessage httpResponse = await client.PostAsync(_configuration.GetSection("Config").GetSection("TelegramUrl").Value.ToString(), null);
            
            if (httpResponse.IsSuccessStatusCode)
            {
                string httpResponseContent = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogInformation(httpResponseContent);
            } else
            {
                string httpResponseContent = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogError("ERROR: " + httpResponseContent + " : " + httpResponse.StatusCode);
            }
        }
    }
}