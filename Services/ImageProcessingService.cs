using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace cat_detector.Services
{
    public class ImageProcessingService
    {
        private readonly ILogger<ImageProcessingService> _logger;
        private IConfiguration _configuration;

        public ImageProcessingService(ILogger<ImageProcessingService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void CropImage(string imageLocation)
        {
            _logger.LogInformation("CropImage() called");
            int x = 0;
            int y = 1420;
            int width = 500;
            int height = 500;
            using (Image image = Image.Load(imageLocation))
            {
                image.Mutate(i => i.Crop(new Rectangle(x, y, width, height)));
                image.Save(imageLocation);
            }
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
