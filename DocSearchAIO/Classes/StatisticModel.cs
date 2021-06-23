using System;
using System.IO;
using System.Linq;
using CSharpFunctionalExtensions;
using DocSearchAIO.Statistics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DocSearchAIO.Classes
{
    public abstract class StatisticModel
    {
        private readonly ILogger _logger;

        protected abstract string DerivedModelName { get; }
        protected abstract string StatisticsDirectory { get; }

        public string GetStatisticFileName => $"statistics_{DerivedModelName}.txt";
        
        protected StatisticModel(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<StatisticModel>();
        }

        protected StatisticModel()
        {
            
        }
        
        public Maybe<ProcessingJobStatistic> GetLatestJobStatisticByModel()
        {
            var statisticsFile = $"statistics_{DerivedModelName}.txt";
            var filePath = $"{StatisticsDirectory}/{statisticsFile}";
            _logger.LogInformation("load statistics information from {FilePath} for model {DerivedModelName}", filePath, DerivedModelName);
            var content = File.ReadAllText(filePath);
            if (!content.Any())
                return Maybe<ProcessingJobStatistic>.None;
            try
            {
                var model = JsonConvert.DeserializeObject<ProcessingJobStatistic>(content);
                return Maybe<ProcessingJobStatistic>.From(model);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "error occured while converting statistic json to model");
                return Maybe<ProcessingJobStatistic>.None;
            }
        }
    }

    public class StatisticModelWord : StatisticModel
    {
        protected override string DerivedModelName => GetType().Name;
        protected override string StatisticsDirectory { get; }

        public StatisticModelWord()
        {
            
        }
        
        public StatisticModelWord(ILoggerFactory loggerFactory, string statisticsDirectory) :
            base(loggerFactory)
        {
            StatisticsDirectory = statisticsDirectory;
        }
    }

    public class StatisticModelPowerpoint : StatisticModel
    {
        protected override string DerivedModelName => GetType().Name;
        protected override string StatisticsDirectory { get; }
        
        public StatisticModelPowerpoint() {}

        public StatisticModelPowerpoint(ILoggerFactory loggerFactory, string statisticsDirectory) : base(loggerFactory)
        {
            StatisticsDirectory = statisticsDirectory;
        }
    }

    public class StatisticModelPdf : StatisticModel
    {
        protected override string DerivedModelName => GetType().Name;
        protected override string StatisticsDirectory { get; }

        public StatisticModelPdf(){}
        public StatisticModelPdf(ILoggerFactory loggerFactory, string statisticsDirectory) :
            base(loggerFactory)
        {
            StatisticsDirectory = statisticsDirectory;
        }
    }
}