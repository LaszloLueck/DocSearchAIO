using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.Statistics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DocSearchAIO.Scheduler
{
    public static class StatisticUtilitiesProxy
    {
        public static readonly Func<ILoggerFactory,
                IEnumerable<KeyValuePair<IProcessorType, Func<Maybe<ProcessingJobStatistic>>>>>
            AsIEnumerable = (loggerFactory) =>
            {
                return new[]
                {
                    KeyValuePair.Create<IProcessorType, Func<Maybe<ProcessingJobStatistic>>>(
                        new ProcessorTypeWord(),
                        () => WordStatisticUtility(loggerFactory).GetLatestJobStatisticByModel()),
                    KeyValuePair.Create<IProcessorType, Func<Maybe<ProcessingJobStatistic>>>(
                        new ProcessorTypePowerPoint(),
                        () => PowerpointStatisticUtility(loggerFactory).GetLatestJobStatisticByModel()),
                    KeyValuePair.Create<IProcessorType, Func<Maybe<ProcessingJobStatistic>>>(
                        new ProcessorTypePdf(),
                        () => PdfStatisticUtility(loggerFactory).GetLatestJobStatisticByModel())
                };
            };

        public static readonly Func<ILoggerFactory, StatisticUtilities<ProcessorTypeWord>>
            WordStatisticUtility = loggerFactory =>
                new StatisticUtilities<ProcessorTypeWord>(loggerFactory);

        public static readonly Func<ILoggerFactory, StatisticUtilities<ProcessorTypePowerPoint>>
            PowerpointStatisticUtility = loggerFactory =>
                new StatisticUtilities<ProcessorTypePowerPoint>(loggerFactory);

        public static readonly Func<ILoggerFactory, StatisticUtilities<ProcessorTypePdf>> PdfStatisticUtility =
            loggerFactory => new StatisticUtilities<ProcessorTypePdf>(loggerFactory);
    }

    public class StatisticUtilities<TModel> where TModel : IProcessorType
    {
        private const string StatisticsDirectory = "./Resources/statistics";
        private const string StatisticsFile = "statistics_{0}.txt";
        private readonly ILogger _logger;
        private readonly InterlockedCounter _entireDocuments;
        private readonly InterlockedCounter _failedDocuments;
        private readonly InterlockedCounter _changedDocuments;

        public StatisticUtilities(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<StatisticUtilities<TModel>>();
            _entireDocuments = new InterlockedCounter();
            _failedDocuments = new InterlockedCounter();
            _changedDocuments = new InterlockedCounter();
            _logger.LogInformation("initialize StatisticUtilities for type {TModel}", typeof(TModel).Name);
            _checkAndCreateStatisticsDirectory();
            _checkAndCreateStatisticsFile();
        }

        private void _checkAndCreateStatisticsDirectory()
        {
            _logger.LogInformation("check if directory {Directory} exists", StatisticsDirectory);
            if (!Directory.Exists(StatisticsDirectory))
                Directory.CreateDirectory(StatisticsDirectory);
        }

        private void _checkAndCreateStatisticsFile()
        {
            var filePath = $"{StatisticsDirectory}/{string.Format(StatisticsFile, typeof(TModel).Name)}";
            _logger.LogInformation("check if file {File} exists", filePath);
            if (!File.Exists(filePath))
                File
                    .Create(filePath)
                    .Dispose();
        }

        public void AddToEntireDocuments() => _entireDocuments.Increment();
        public void AddToFailedDocuments() => _failedDocuments.Increment();
        public void AddToChangedDocuments(int value) => _changedDocuments.Add(value);

        public int GetEntireDocumentsCount() => _entireDocuments.GetCurrent();
        public int GetFailedDocumentsCount() => _failedDocuments.GetCurrent();
        public int GetChangedDocumentsCount() => _changedDocuments.GetCurrent();

        public void AddJobStatisticToDatabase(ProcessingJobStatistic jobStatistic)
        {
            var filePath = $"{StatisticsDirectory}/{string.Format(StatisticsFile, typeof(TModel).Name)}";
            var json = JsonConvert.SerializeObject(jobStatistic, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public Maybe<ProcessingJobStatistic> GetLatestJobStatisticByModel()
        {
            var filePath = $"{StatisticsDirectory}/{string.Format(StatisticsFile, typeof(TModel).Name)}";
            _logger.LogInformation("load statistics information from {FilePath} for model {TModel}", filePath,
                typeof(TModel).Name);
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
}