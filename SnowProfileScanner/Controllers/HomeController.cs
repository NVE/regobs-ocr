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
        private readonly SnowProfileService _snowProfileService;

        public HomeController(IConfiguration configuration, SnowProfileService snowProfileService)
        {
            _configuration = configuration;
            _snowProfileService = snowProfileService;
        }

        [HttpGet]
        public async Task<IActionResult> ViewAllEntries()
        {
            List<SnowProfileEntity> results = await _snowProfileService.GetAll();

            return View("ViewAllResults", results);
        }

       

        [HttpGet]
        public async Task<IActionResult> ShowProfile(string id)
        {
            SnowProfileEntity? snowProfileEntity = await _snowProfileService.Get(id);

            if (snowProfileEntity != null)
            {
                return View("Result", snowProfileEntity);
            }
            else
            {
                // Handle the case where no matching entity is found
                return NotFound();
            }
        }

       
    }
}
