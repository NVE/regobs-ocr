using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using SnowProfileScanner.Models;
using SnowProfileScanner.Services;

namespace SnowProfileScanner.Controllers
{
    public class HomeController: Controller
    {
        private readonly IConfiguration _configuration;
        private readonly SnowProfileService _temperatureProfileService;

        public HomeController(IConfiguration configuration, SnowProfileService temperatureProfileService)
        {
            _configuration = configuration;
            _temperatureProfileService = temperatureProfileService;
        }

        [HttpGet]
        public async Task<IActionResult> ViewAllEntries()
        {
            List<SnowProfileEntity> results = await _temperatureProfileService.GetAll();

            return View("ViewAllResults", results);
        }

       

        [HttpGet]
        public async Task<IActionResult> ShowProfile(string id)
        {
            SnowProfileEntity? temperatureProfileEntity = await _temperatureProfileService.Get(id);

            if (temperatureProfileEntity != null)
            {
                // Convert TemperatureProfileEntity to TemperatureProfile
                var temperatureProfile = new SnowProfile
                {
                    // Populate properties based on temperatureProfileEntity properties
                    AirTemp = temperatureProfileEntity.SnowProfile.AirTemp,
                    Layers = temperatureProfileEntity.SnowProfile.Layers, 
                    SnowTemp = temperatureProfileEntity.SnowProfile.SnowTemp 
                };

                return View("Result", temperatureProfile);
            }
            else
            {
                // Handle the case where no matching entity is found
                return NotFound();
            }
        }

       
    }
}
