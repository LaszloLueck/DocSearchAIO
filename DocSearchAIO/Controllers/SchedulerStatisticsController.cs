using System.Collections.Generic;
using System.Threading.Tasks;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.DocSearch.TOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SchedulerStatisticsController : ControllerBase
    {

        private readonly ILogger _logger;
        private readonly SchedulerStatisticsService _schedulerStatisticsService;

        public SchedulerStatisticsController(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<SchedulerStatisticsController>();
            _schedulerStatisticsService = new SchedulerStatisticsService(loggerFactory, configuration);
        }
        
        
        [HttpPost]
        public async Task<IEnumerable<SchedulerStatistics>> GetSchedulerStatistics()
        {
            return await _schedulerStatisticsService.GetSchedulerStatistics();
        }
    }
}