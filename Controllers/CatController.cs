using Microsoft.AspNetCore.Mvc;

namespace cat_detector.Controllers
{
    [ApiController]
    [Route("/")]
    public class CatController : ControllerBase
    {
        private readonly ILogger<CatController> _logger;

        public CatController(ILogger<CatController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public string Get()
        {
            return "cat";
        }
    }
}