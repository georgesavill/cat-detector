using cat_detector.Classes;

namespace cat_detector.Services
{
    public class TelegramService
    {
        private readonly ILogger<TelegramService> _logger;
        private IConfiguration _configuration;

        public TelegramService(ILogger<TelegramService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration; 
        }

        public async void SendMessage(string telegramId, string message)
        {
            _logger.LogDebug("SendMessage() called with ID: {0} and message: {1}", telegramId, message);

            HttpClient client = new HttpClient();
            HttpResponseMessage httpResponse = await client.PostAsync(_configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>().TelegramUrl + "?chat_id=" + telegramId + "&text=" + message, null);

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
