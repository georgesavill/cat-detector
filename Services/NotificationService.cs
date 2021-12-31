using cat_detector.Classes;

namespace cat_detector.Services
{
    public class NotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private ConfigurationOptions _configurationOptions;

        public NotificationService(ILogger<NotificationService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configurationOptions = configuration.GetSection(ConfigurationOptions.Config).Get<ConfigurationOptions>();
        }

        public async void SendTelegramMessage(string telegramId, string message)
        {
            _logger.LogDebug("SendTelegramMessage() called with ID: {0} and message: {1}", telegramId, message);

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

        public async void TriggerWebhook(string webhook)
        {
            _logger.LogDebug("TriggerWebhook() called with URL: {0}", webhook);
            HttpClient client = new HttpClient();
            HttpResponseMessage httpResponse = await client.PostAsync(webhook, null);

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
