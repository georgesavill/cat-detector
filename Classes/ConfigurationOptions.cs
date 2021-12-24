namespace cat_detector.Classes
{
    public class ConfigurationOptions
    {
        public const string Config = "Config";

        public string CctvAuth { get; set; }
        public string CctvUrl { get; set; }
        public string TelegramUrl { get; set; }
        public TelegramUserClass[] TelegramUsers { get; set; }
        public int ImageCropX { get; set; }
        public int ImageCropY { get; set; }
        public int ImageCropWidth { get; set; }
        public int ImageCropHeight { get; set; }
        public float PredictionThreshold { get; set; }
        public int MinutesBetweenAlerts { get; set; }
        public int MinutesBetweenNoneImageSaved { get; set; }
    }
}