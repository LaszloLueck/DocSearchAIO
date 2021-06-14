using System;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Scheduler
{
    public class JobStatusPersistence<TModel> where TModel : ElasticDocument
    {
        private readonly ILogger _logger;
        private readonly ILiteCollection<JobStatus> _collection;

        public JobStatusPersistence(ILoggerFactory loggerFactory, ILiteDatabase liteDatabase)
        {
            _logger = loggerFactory.CreateLogger<JobStatusPersistence<TModel>>();
            _collection = liteDatabase.GetCollection<JobStatus>($"jobStatus_{typeof(TModel).Name}");
            _collection.EnsureIndex(t => t.ForJob);
        }

        public bool RemoveEntry()
        {
            _logger.LogInformation("remove job status entry for model {Model}", typeof(TModel).Name);
            return _collection.Delete(typeof(TModel).Name);
        }

        public bool AddEntry(JobState state)
        {
            _logger.LogInformation("add a new job status {State} for model {Model}", state.ToString(),
                typeof(TModel).Name);
            var jobState = new JobStatus()
            {
                Id = typeof(TModel).Name,
                ForJob = typeof(TModel).Name,
                JobState = state,
                WriteDate = DateTime.Now
            };
            return _collection.Upsert(jobState);
        }

        public Maybe<JobStatus> GetEntry()
        {
            _logger.LogInformation("get job status for model {Model}", typeof(TModel).Name);
            return _collection
                .Query()
                .Where(t => t.ForJob == typeof(TModel).Name)
                .ToEnumerable()
                .TryFirst();
        }
    }

    public enum JobState
    {
        Running,
        Stopped
    }

    public class JobStatus
    {
        public string Id { get; set; }
        public string ForJob { get; set; }
        public JobState JobState { get; set; }
        public DateTime WriteDate { get; set; }
    }
}