using System.Threading.Tasks;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.DocSearch.TOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nest;

namespace DocSearchAIO.Controllers
{    
    [ApiController]
    [Route("[controller]")]
    public class DocumentDetailController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly DocumentDetailService _documentDetailService;

        public DocumentDetailController(IElasticClient elasticClient, ILoggerFactory loggerFactory, ViewToStringRenderer viewToStringRenderer)
        {
            _logger = loggerFactory.CreateLogger<DoSearchController>();
            _documentDetailService = new DocumentDetailService(elasticClient, loggerFactory, viewToStringRenderer);
        }
        
        [HttpPost]
        public async Task<DocumentDetailResponse> GetDocumentDetail(DocumentDetailRequest request)
        {
            _logger.LogInformation($"get documentdetails for {request.Id}");
            return await _documentDetailService.GetDocumentDetail(request);
        }
    }
}