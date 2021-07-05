using System;
using System.IO;
using System.Linq;
using CSharpFunctionalExtensions;
using DocSearchAIO.Scheduler;
using DocSearchAIO.Statistics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DocSearchAIO.Classes
{
    public abstract class StatisticModel
    {
        private readonly ILogger _logger;

        protected abstract string DerivedModelName { get; }
        protected abstract TypedDirectoryPathString StatisticsDirectory { get; }

        public TypedFileNameString StatisticFileName => new($"statistics_{DerivedModelName}.txt");

        protected StatisticModel(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<StatisticModel>();
        }

        protected StatisticModel()
        {
        }

        public Maybe<ProcessingJobStatistic> LatestJobStatisticByModel()
        {
            var filePath = $"{StatisticsDirectory}/{StatisticFileName}";
            _logger.LogInformation("load statistics information from {FilePath} for model {DerivedModelName}", filePath,
                DerivedModelName);
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
        protected override TypedDirectoryPathString StatisticsDirectory { get; }

        public StatisticModelWord()
        {
        }

        public StatisticModelWord(ILoggerFactory loggerFactory, TypedDirectoryPathString statisticsDirectory) :
            base(loggerFactory)
        {
            StatisticsDirectory = statisticsDirectory;
        }
    }

    public class StatisticModelPowerpoint : StatisticModel
    {
        protected override string DerivedModelName => GetType().Name;
        protected override TypedDirectoryPathString StatisticsDirectory { get; }

        public StatisticModelPowerpoint()
        {
        }

        public StatisticModelPowerpoint(ILoggerFactory loggerFactory, TypedDirectoryPathString statisticsDirectory) : base(loggerFactory)
        {
            StatisticsDirectory = statisticsDirectory;
        }
    }

    public class StatisticModelPdf : StatisticModel
    {
        protected override string DerivedModelName => GetType().Name;
        protected override TypedDirectoryPathString StatisticsDirectory { get; }

        public StatisticModelPdf()
        {
        }

        public StatisticModelPdf(ILoggerFactory loggerFactory, TypedDirectoryPathString statisticsDirectory) :
            base(loggerFactory)
        {
            StatisticsDirectory = statisticsDirectory;
        }
    }

    public class StatisticModelExcel : StatisticModel
    {
        protected override string DerivedModelName => GetType().Name;
        protected override TypedDirectoryPathString StatisticsDirectory { get; }

        public StatisticModelExcel()
        {
        }

        public StatisticModelExcel(ILoggerFactory loggerFactory, TypedDirectoryPathString statisticsDirectory) :
            base(loggerFactory)
        {
            StatisticsDirectory = statisticsDirectory;
        }
    }
}