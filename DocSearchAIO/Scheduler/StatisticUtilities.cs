using System;
using System.Linq;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.Statistics;
using LiteDB;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DocSearchAIO.Scheduler
{
    public class StatisticUtilities<TModel> : IDisposable where TModel : ElasticDocument
    {
        private readonly ILogger _logger;
        private readonly ILiteDatabase _liteDatabase;

        private readonly InterlockedCounter _entireDocuments;
        private readonly InterlockedCounter _failedDocuments;
        private readonly InterlockedCounter _changedDocuments;

        public StatisticUtilities(ILoggerFactory loggerFactory, ILiteDatabase liteDatabase)
        {
            _logger = loggerFactory.CreateLogger<StatisticUtilities<TModel>>();
            _liteDatabase = liteDatabase;
            _entireDocuments = new InterlockedCounter();
            _failedDocuments = new InterlockedCounter();
            _changedDocuments = new InterlockedCounter();
        }

        public void AddToEntireDocuments()  => _entireDocuments.Increment();
        public void AddToFailedDocuments() => _failedDocuments.Increment();
        public void AddToChangedDocuments(int value) => _changedDocuments.Add(value);

        public int GetEntireDocumentsCount() => _entireDocuments.GetCurrent();
        public int GetFailedDocumentsCount() => _failedDocuments.GetCurrent();
        public int GetChangedDocumentsCount() => _changedDocuments.GetCurrent();
        
        public void AddJobStatisticToDatabase(ProcessingJobStatistic jobStatistic)
        {
            var liteCollection = _liteDatabase.GetCollection<ProcessingJobStatistic>(typeof(TModel).Name);
            _logger.LogInformation("write statistic for {Type}", typeof(TModel).Name);
            var json = JsonConvert.SerializeObject(jobStatistic, Formatting.Indented);
            _logger.LogInformation("write {Object}", json);
            liteCollection.Insert(jobStatistic);
        }

        public Maybe<ProcessingJobStatistic> GetLatestJobStatisticByModel()
        {
            var liteCollection = _liteDatabase.GetCollection<ProcessingJobStatistic>(typeof(TModel).Name);
            _logger.LogInformation("get statistic for {Type}", typeof(TModel).Name);
            return liteCollection
                .FindAll()
                .OrderByDescending(d => d.StartJob)
                .TryFirst();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}