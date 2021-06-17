using System;
using System.Collections;
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
        public static readonly Func<ILoggerFactory, StatisticUtilities<WordElasticDocument>>
            WordStatisticUtility = loggerFactory =>
                new StatisticUtilities<WordElasticDocument>(loggerFactory);

        public static readonly Func<ILoggerFactory, StatisticUtilities<PowerpointElasticDocument>>
            PowerpointStatisticUtility = loggerFactory =>
                new StatisticUtilities<PowerpointElasticDocument>(loggerFactory);

        public static readonly Func<ILoggerFactory, StatisticUtilities<PdfElasticDocument>> PdfStatisticUtility =
            loggerFactory => new StatisticUtilities<PdfElasticDocument>(loggerFactory);
    }

    public class StatisticUtilities<TModel> where TModel : ElasticDocument
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
            _checkAndCreateStatisticsDirectory();
            _checkAndCreateStatisticsFile();
        }

        private readonly Action _checkAndCreateStatisticsDirectory = () =>
        {
            if (!Directory.Exists(StatisticsDirectory))
                Directory.CreateDirectory(StatisticsDirectory);
        };

        private readonly Action _checkAndCreateStatisticsFile = () =>
        {
            var filePath = $"{StatisticsDirectory}/{string.Format(StatisticsFile, typeof(TModel).Name)}";
            if (!File.Exists(filePath))
                File
                    .Create(filePath)
                    .Dispose();
        };

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
            var content = File.ReadAllText(filePath);
            if(!content.Any())
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