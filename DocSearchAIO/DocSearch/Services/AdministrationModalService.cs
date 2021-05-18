using System.Threading.Tasks;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.TOs;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.DocSearch.Services
{
    public class AdministrationModalService
    {
        private readonly ILogger _logger;
        private readonly ViewToStringRenderer _viewToStringRenderer;
        public AdministrationModalService(ILoggerFactory loggerFactory, ViewToStringRenderer viewToStringRenderer)
        {
            _logger = loggerFactory.CreateLogger<AdministrationModalService>();
            _viewToStringRenderer = viewToStringRenderer;
        }

        public async Task<AdministrationModalResponse> GetAdministrationModal()
        {
            var content = await _viewToStringRenderer.Render("AdministrationModalPartial", new {});
            return new AdministrationModalResponse() {Content = content, ElementName = "#adminModal"};
        }
    }
}