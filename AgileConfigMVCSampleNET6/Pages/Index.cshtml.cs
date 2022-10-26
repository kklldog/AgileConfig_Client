using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace AgileConfigMVCSampleNET6.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly IOptionsMonitor<GlobalOptions> _optionsMonitor;

        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration, IOptionsMonitor<GlobalOptions> optionsMonitor)
        {
            _logger = logger;
            logger.LogInformation("test logger write .");
            _configuration = configuration;
            _optionsMonitor = optionsMonitor;

            Console.WriteLine(_optionsMonitor.CurrentValue.OptionsTest);
        }

        public void OnGet()
        {

        }
    }
}