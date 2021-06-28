using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Classes
{
    public class ReverseComparerService<T> where T : ComparerModel
    {
        private readonly ILogger _logger;
        private readonly string _comparerFile;

        public ReverseComparerService(ILoggerFactory loggerFactory, T model)
        {
            _logger = loggerFactory.CreateLogger<ReverseComparerService<T>>();
            _comparerFile = model.GetComparerFilePath;
        }

        public async Task Process(string indexName)
        {
            await Task.Run(() =>
            {
                _logger.LogInformation("process comparer file {ComparerFile}", _comparerFile);
                _logger.LogInformation("process elastic index {IndexName}", indexName);
            });
        }
        
    }
}