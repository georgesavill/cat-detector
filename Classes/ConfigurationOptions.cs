namespace cat_detector.Classes
{
    public class ConfigurationOptions
    {
        public const string Config = "Config";

        public string CctvAuth { get; set; }
        public string CctvUrl { get; set; }
        public string TelegramUrl { get; set; }
        public TelegramUserClass[] TelegramUsers { get; set; }
    }
}