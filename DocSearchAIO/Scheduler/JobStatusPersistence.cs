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
            _logger.LogInformation("create litedb instance for jobStatus {JobStatus}", typeof(TModel).Name);
            _collection = liteDatabase.GetCollection<JobStatus>($"jobStatus");
            _collection.EnsureIndex(t => t.ForJob);
        }

        public void RemoveEntry()
        {
            _logger.LogInformation("remove job status entry for model {Model}", typeof(TModel).Name);
            _collection.Delete(typeof(TModel).Name);
        }

        public async Task AddEntry(JobState state)
        {
            await Task.Run(() =>
            {
                _logger.LogInformation("set job status to {State} for model {Model}", state.ToString(),
                    typeof(TModel).Name);
                var jobState = new JobStatus()
                {
                    Id = typeof(TModel).Name,
                    ForJob = typeof(TModel).Name,
                    JobState = state,
                    WriteDate = DateTime.Now
                };
                var retVal = _collection.Upsert(jobState.Id, jobState);
                _logger.LogInformation("set job status resulted in {Result}", retVal);
            });
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