using System.Threading.Tasks;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.DocSearch.TOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;

namespace DocSearchAIO.DocSearch.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public partial class OptionDialogController : ControllerBase
    {

        private readonly ILogger _logger;
        private readonly OptionDialogService _optionDialogService;
        
        public OptionDialogController(ILoggerFactory loggerFactory, ViewToStringRenderer viewToStringRenderer, IElasticClient elasticClient)
        {
            _logger = loggerFactory.CreateLogger<OptionDialogController>();
            _optionDialogService = new OptionDialogService(loggerFactory, viewToStringRenderer, elasticClient);
        }

        [HttpPost]
        public async Task<OptionDialogResponse> OptionDialog(OptionDialogRequest optionDialogRequest)
        {
            _logger.LogInformation("method called!");
            return await _optionDialogService.GetOptionDialog(optionDialogRequest);
        }


        public class OptionDialogResponse
        {
            public string Content { get; set; }
            public string State { get; set; }
        
            public string ElementName { get; set; }
        }
        
    }
}