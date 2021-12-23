using cat_detector.Services;
using Microsoft.AspNetCore.Mvc;

namespace cat_detector.Controllers
{
    [ApiController]
    [Route("/")]
    public class CatController : ControllerBase
    {
        private readonly ILogger<CatController> _logger;
        private PredictionService _predictionService;

        public CatController(ILogger<CatController> logger, PredictionService predictionService)
        {
            _logger = logger;
            _predictionService = predictionService;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            _logger.LogInformation("Get recieved");
            return await _predictionService.MakePrediction();
        }
    }
}