using HomeAssignment.Factories;
using Microsoft.AspNetCore.Mvc;

namespace HomeAssignment.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IRepositoryFactory _repositoryFactory;

        public HealthController(IRepositoryFactory repositoryFactory)
        {
            _repositoryFactory = repositoryFactory;
        }

        /// <summary>
        /// Health check endpoint with storage information
        /// </summary>
        /// <returns>Health status and current storage type</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetHealth()
        {
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                StorageType = _repositoryFactory.GetCurrentStorageType(),
                Version = "1.0.0"
            });
        }
    }
}
