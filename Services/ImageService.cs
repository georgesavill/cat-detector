using cat_detector.Classes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Text;

namespace cat_detector.Services
{
    public class ImageService
    {
        private readonly ILogger<ImageService> _logger;
        private IConfiguration _configuration;

        public ImageService(ILogger<ImageService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void CropImage(string imageLocation)
        {
            _logger.LogInformation("CropImage() called");
            int x = _configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>().ImageCropX;
            int y = _configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>().ImageCropY;
            int width = _configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>().ImageCropWidth;
            int height = _configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>().ImageCropHeight;
            using (Image image = Image.Load(imageLocation))
            {
                image.Mutate(i => i.Crop(new Rectangle(x, y, width, height)));
                image.Save(imageLocation);
            }
        }

        public async Task<(string, string)> DownloadCctvImage()
        {
            _logger.LogInformation("DownloadCctvImage() called");
            string imageFilename = DateTimeOffset.Now.ToUnixTimeSeconds() + ".jpg";
            string imageLocation = "/media/" + imageFilename;
            HttpClient httpClient = new HttpClient();
            var byteArray = new UTF8Encoding().GetBytes(_configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>().CctvAuth);
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            using (var contentStream = await httpClient.GetStreamAsync(_configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>().CctvUrl))
            using (var fileStream = new FileStream(imageLocation, FileMode.Create, FileAccess.Write, FileShare.None, 1048576, true))
            {
                await contentStream.CopyToAsync(fileStream);
            }

            return (imageLocation, imageFilename);
        }

        public void MoveImage(string originalImageLocation, string newImageLocation)
        {
            _logger.LogInformation("MoveImage() called");
            try
            {
                if (!File.Exists(originalImageLocation))
                {
                    // This statement ensures that the file is created, but the handle is not kept.
                    using (FileStream fs = File.Create(originalImageLocation)) { }
                }

                // Ensure that the target does not exist.
                if (File.Exists(newImageLocation))
                    File.Delete(newImageLocation);

                // Move the file.
                File.Move(originalImageLocation, newImageLocation);
                _logger.LogInformation("{0} was moved to {1}.", originalImageLocation, newImageLocation);

                // See if the original exists now.
                if (File.Exists(originalImageLocation))
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
    }
}
