using Cat_detector;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
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
            var sampleData = new MLModel.ModelInput()
            {
                ImageSource = await GetImageLocation(),
            };

            string catStatus = await Task.FromResult(MLModel.Predict(sampleData).Prediction);
            if (catStatus == "cat")
            {
                SendNotification();
            }
            _logger.LogInformation("Returning status: " + catStatus);
            return catStatus;
        }

        async Task<string> GetImageLocation()
        {
            _logger.LogInformation("GetImageLocation() called");
            string imageLocation = "/media/" + DateTimeOffset.Now.ToUnixTimeSeconds() + ".jpg";
            HttpClient httpClient = new HttpClient();
            var byteArray = new UTF8Encoding().GetBytes(_configuration.GetSection("Config").GetSection("CctvAuth").Value.ToString());
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            using (var contentStream = await httpClient.GetStreamAsync(_configuration.GetSection("Config").GetSection("CctvUrl").Value.ToString()))
            using (var fileStream = new FileStream(imageLocation, FileMode.Create, FileAccess.Write, FileShare.None, 1048576, true))
            {
                await contentStream.CopyToAsync(fileStream);
            }

            CropImage(imageLocation);

            return imageLocation;
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