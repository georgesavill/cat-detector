using cat_detector.Services;
using Microsoft.AspNetCore.Mvc;

namespace cat_detector.Controllers
{
    [ApiController]
    [Route("/")]
    public class PredictionController : ControllerBase
    {
        private readonly ILogger<PredictionController> _logger;
        private PredictionService _predictionService;

        public PredictionController(ILogger<PredictionController> logger, PredictionService predictionService)
        {
            _logger = logger;
            _predictionService = predictionService;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            //_logger.LogDebug("Get recieved");
            return await _predictionService.MakePrediction();
        }
    }
}