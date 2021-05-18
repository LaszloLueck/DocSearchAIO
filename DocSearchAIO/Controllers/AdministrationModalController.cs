using System.Threading.Tasks;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.DocSearch.TOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AdministrationModalController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly AdministrationModalService _administrationModalService;
        public AdministrationModalController(ILoggerFactory loggerFactory, ViewToStringRenderer viewToStringRenderer)
        {
            _logger = loggerFactory.CreateLogger<AdministrationModalController>();
            _administrationModalService = new AdministrationModalService(loggerFactory, viewToStringRenderer);
        }
        
        
        [HttpPost]
        public async Task<AdministrationModalResponse> GetAdministrationModal()
        {
            return await _administrationModalService.GetAdministrationModal();
        }
    }
}