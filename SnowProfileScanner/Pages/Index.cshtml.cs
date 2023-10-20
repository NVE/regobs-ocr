using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SnowProfileScanner.Pages
{
	public class IndexModel : PageModel
	{
		private readonly ILogger<IndexModel> _logger;
        public readonly bool useCaptcha;
		public readonly string siteKey;

		public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration)
		{
			_logger = logger;
			useCaptcha = bool.Parse(configuration["UseCaptcha"]);
			siteKey = configuration["SiteKey"];
		}

		public void OnGet()
		{

		}
	}
}