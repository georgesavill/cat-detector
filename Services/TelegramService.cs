using cat_detector.Classes;

namespace cat_detector.Services
{
    public class TelegramService
    {
        private readonly ILogger<TelegramService> _logger;
        private ConfigurationOptions _configurationOptions;

        public TelegramService(ILogger<TelegramService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configurationOptions = configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>();
        }

        public async void SendMessage(string telegramId, string message)
        {
            _logger.LogDebug("SendMessage() called with ID: {0} and message: {1}", telegramId, message);

            HttpClient client = new HttpClient();
            HttpResponseMessage httpResponse = await client.PostAsync(_configurationOptions.TelegramUrl + "?chat_id=" + telegramId + "&text=" + message, null);

            if (httpResponse.IsSuccessStatusCode)
            {
                string httpResponseContent = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogDebug(httpResponseContent);
            }
            else
            {
                string httpResponseContent = await httpResponse.Content.ReadAsStringAsync();
                _logger.LogError("ERROR: {0} : {1}", httpResponseContent, httpResponse.StatusCode);
            }
        }
    }
}
