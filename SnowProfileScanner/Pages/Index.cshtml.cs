using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SnowProfileScanner.Pages
{
	public class IndexModel : PageModel
	{
		private readonly ILogger<IndexModel> _logger;
        public readonly bool _useCaptcha;

		public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration)
		{
			_logger = logger;
			_useCaptcha = bool.Parse(configuration["UseCaptcha"]);
		}

		public void OnGet()
		{

		}
	}
}