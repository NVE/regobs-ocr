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
        private readonly TemperatureProfileService _temperatureProfileService;

        public HomeController(IConfiguration configuration, TemperatureProfileService temperatureProfileService)
        {
            _configuration = configuration;
            _temperatureProfileService = temperatureProfileService;
        }

        [HttpGet]
        public async Task<IActionResult> ViewAllEntries()
        {
            List<TemperatureProfileEntity> results = await _temperatureProfileService.GetAll();

            return View("ViewAllResults", results);
        }

       

        [HttpGet]
        public async Task<IActionResult> ShowProfile(string id)
        {
            TemperatureProfileEntity? temperatureProfileEntity = await _temperatureProfileService.Get(id);

            if (temperatureProfileEntity != null)
            {
                // Convert TemperatureProfileEntity to TemperatureProfile
                var temperatureProfile = new TemperatureProfile
                {
                    // Populate properties based on temperatureProfileEntity properties
                    AirTemp = temperatureProfileEntity.TemperatureProfile.AirTemp,
                    Layers = temperatureProfileEntity.TemperatureProfile.Layers, // Assuming Layers is a collection in TemperatureProfileEntity
                    SnowTemp = temperatureProfileEntity.TemperatureProfile.SnowTemp // Assuming SnowTemp is a collection in TemperatureProfileEntity
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
