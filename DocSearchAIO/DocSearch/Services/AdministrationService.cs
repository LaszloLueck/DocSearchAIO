using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Scheduler;
using DocSearchAIO.Services;
using DocSearchAIO.Statistics;
using DocSearchAIO.Utilities;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.Extensions.Caching.Memory;
using Nest;
using Quartz;
using JobState = DocSearchAIO.Scheduler.JobState;
using ProcessorBase = DocSearchAIO.Classes.ProcessorBase;

namespace DocSearchAIO.DocSearch.Services;

public class AdministrationService
{
    private readonly ILogger _logger;
    private readonly ConfigurationObject _configurationObject;
    private readonly IElasticSearchService _elasticSearchService;
    private readonly SchedulerStatisticsService _schedulerStatisticsService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly ElasticUtilities _elasticUtilities;
    private readonly MemoryCacheModelProxy _memoryCacheModelProxy;

    public AdministrationService(ILoggerFactory loggerFactory,
        IConfiguration configuration, IElasticSearchService elasticSearchService, IMemoryCache memoryCache)
    {
        _logger = loggerFactory.CreateLogger<AdministrationService>();
        var cfgTmp = new ConfigurationObject();
        configuration.GetSection("configurationObject").Bind(cfgTmp);
        _configurationObject = cfgTmp;
        _elasticSearchService = elasticSearchService;
        _schedulerStatisticsService = new SchedulerStatisticsService(loggerFactory, configuration);
        _elasticUtilities = new ElasticUtilities(loggerFactory, elasticSearchService);
        _memoryCacheModelProxy = new MemoryCacheModelProxy(loggerFactory, memoryCache);
        _loggerFactory = loggerFactory;
        _memoryCache = memoryCache;
    }

    public async Task<bool> PauseTriggerWithTriggerId(TriggerStateRequest triggerStateRequest)
    {
        var schedulerOpt = await SchedulerUtilities.StdSchedulerByName(_configurationObject.SchedulerName);
        return await schedulerOpt.Match(
            async scheduler =>
            {
                var triggerKey = new TriggerKey(triggerStateRequest.TriggerId, triggerStateRequest.GroupId);
                var result = await ConfigurationTuple(_configurationObject)
                    .Where(tpl => tpl.TriggerName == triggerKey.Name)
                    .TryFirst()
                    .Match(
                        async currentSelected =>
                        {
                            _logger.LogInformation("pause trigger for {TriggerName} :: {TriggerKey}",
                                currentSelected.Key, triggerKey.Name);
                            switch (currentSelected.Item3)
                            {
                                case "processing":
                                    _configurationObject.Processing[currentSelected.Key].Active = false;
                                    break;
                                case "cleanup":
                                    _configurationObject.Cleanup[currentSelected.Key].Active = false;
                                    break;
                            }

                            await ConfigurationUpdater.UpdateConfigurationObject(_configurationObject, true);
                            return await scheduler.GetTriggerState(triggerKey) == TriggerState.Paused;
                        },
                        async () => await Task.Run(() => false)
                    );
                await scheduler.PauseTrigger(triggerKey);
                return result;
            },
            async () =>
            {
                _logger.LogWarning("Cannot find scheduler with name {SchedulerName}",
                    _configurationObject.SchedulerName);
                return await Task.Run(() => false);
            });
    }

    private static readonly
        Func<ConfigurationObject, IEnumerable<(string Key, string TriggerName, string SchedulerType)>>
        ConfigurationTuple =
            configurationObject =>
            {
                var processingTuples =
                    configurationObject.Processing.SelectKv((key, value) => (key, value.TriggerName, "processing"));
                var cleanupTuples =
                    configurationObject.Cleanup.SelectKv((key, value) => (key, value.TriggerName, "cleanup"));
                return processingTuples.Concat(cleanupTuples);
            };

    public async Task<bool> ResumeTriggerWithTriggerId(TriggerStateRequest triggerStateRequest)
    {
        var schedulerOpt = await SchedulerUtilities.StdSchedulerByName(_configurationObject.SchedulerName);
        return await schedulerOpt.Match(
            async scheduler =>
            {
                var triggerKey = new TriggerKey(triggerStateRequest.TriggerId, triggerStateRequest.GroupId);
                var result = await ConfigurationTuple(_configurationObject)
                    .Where(tpl => tpl.TriggerName == triggerKey.Name)
                    .TryFirst()
                    .Match(
                        async currentSelected =>
                        {
                            _logger.LogInformation("resume trigger for {TriggerName} :: {TriggerKey}",
                                currentSelected.Key, triggerKey.Name);
                            switch (currentSelected.SchedulerType)
                            {
                                case "processing":
                                    _configurationObject.Processing[currentSelected.Key].Active = true;
                                    break;
                                case "cleanup":
                                    _configurationObject.Cleanup[currentSelected.Key].Active = true;
                                    break;
                            }

                            await ConfigurationUpdater.UpdateConfigurationObject(_configurationObject, true);
                            return await scheduler.GetTriggerState(triggerKey) == TriggerState.Normal;
                        },
                        async () => await Task.Run(() => false)
                    );
                await scheduler.ResumeTrigger(triggerKey);
                return result;
            },
            async () =>
            {
                _logger.LogWarning("Cannot find scheduler with name {SchedulerName}",
                    _configurationObject.SchedulerName);
                return await Task.Run(() => false);
            });
    }

    public async Task<bool> InstantStartJobWithJobId(JobStatusRequest jobStatusRequest)
    {
        var schedulerOpt = await SchedulerUtilities.StdSchedulerByName(_configurationObject.SchedulerName);
        return await schedulerOpt.Match(
            async scheduler =>
            {
                _logger.LogInformation("start job for job {Job} in group {Group}", jobStatusRequest.JobName,
                    jobStatusRequest.GroupId);
                var jobKey = new JobKey(jobStatusRequest.JobName, jobStatusRequest.GroupId);
                await scheduler.TriggerJob(jobKey);
                return true;
            },
            async () =>
            {
                _logger.LogWarning("Cannot find scheduler with name {SchedulerName}",
                    _configurationObject.SchedulerName);
                return await Task.Run(() => false);
            });
    }

    public async Task<bool> SetAdministrationGenericContent(AdministrationGenericRequest request)
    {
        try
        {
            _configurationObject.ElasticEndpoints = request.ElasticEndpoints;
            _configurationObject.SchedulerGroupName = request.ProcessorGroupName;
            _configurationObject.IndexName = request.IndexName;
            _configurationObject.ElasticUser = request.ElasticUser;
            _configurationObject.ElasticPassword = request.ElasticPassword;
            _configurationObject.ScanPath = request.ScanPath;
            _configurationObject.SchedulerId = request.SchedulerId;
            _configurationObject.SchedulerName = request.SchedulerName;
            _configurationObject.UriReplacement = request.UriReplacement;
            _configurationObject.ActorSystemName = request.ActorSystemName;
            _configurationObject.ComparerDirectory = request.ComparerDirectory;
            _configurationObject.CleanupGroupName = request.CleanupGroupName;

            _configurationObject.Processing = request
                .ProcessorConfigurations
                .Select(kv => new KeyValuePair<string, SchedulerEntry>(kv.Item1, kv.Item2))
                .ToDictionary();

            _configurationObject.Cleanup = request
                .CleanupConfigurations
                .Select(kv =>
                    new KeyValuePair<string, CleanUpEntry>(kv.Item1, kv.Item2))
                .ToDictionary();

            await ConfigurationUpdater.UpdateConfigurationObject(_configurationObject, true);
            _logger.LogInformation("configuration successfully updated");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "an error while updating the configuration occured");
            return false;
        }
    }

    private Option<ComparerModel> ComparerBaseFromParameter(string parameter)
    {
        try
        {
            var comparerBase = parameter switch
            {
                nameof(WordElasticDocument) =>
                    new ComparerModelWord(_loggerFactory, _configurationObject.ComparerDirectory) as ComparerModel,
                nameof(PowerpointElasticDocument) =>
                    new ComparerModelPowerpoint(_loggerFactory, _configurationObject.ComparerDirectory),
                nameof(PdfElasticDocument) =>
                    new ComparerModelPdf(_loggerFactory, _configurationObject.ComparerDirectory),
                nameof(ExcelElasticDocument) =>
                    new ComparerModelExcel(_loggerFactory, _configurationObject.ComparerDirectory),
                _ => throw new ArgumentOutOfRangeException(nameof(parameter), parameter,
                    $"cannot cast from parameter {parameter}")
            };
            return comparerBase;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "an error while converting a base parameter occured");
            return Option<ComparerModel>.None;
        }
    }

    public async Task<bool> DeleteIndexAndStartJob(JobStatusRequest jobStatusRequest)
    {
        var schedulerOpt = await SchedulerUtilities.StdSchedulerByName(_configurationObject.SchedulerName);
        await schedulerOpt.Match(
            async scheduler =>
            {
                var boolReturn = await _configurationObject
                    .Processing
                    .Where(d => d.Value.JobName == jobStatusRequest.JobName)
                    .TryFirst()
                    .Match(
                        async kv =>
                        {
                            var key = kv.Key;
                            var value = kv.Value;

                            var indexName = _elasticUtilities.CreateIndexName(_configurationObject.IndexName,
                                value.IndexSuffix);

                            _logger.LogInformation("remove index {IndexName}", indexName);
                            await _elasticSearchService.DeleteIndexAsync(indexName);

                            _logger.LogInformation("remove comparer file for key {Key}", key);
                            ComparerBaseFromParameter(key)
                                .Match(
                                    comparerBase => comparerBase.CleanDictionaryAndRemoveComparerFile(),
                                    () => _logger.LogWarning(
                                        "cannot determine correct comparer base from key {Key}", key));

                            _logger.LogInformation("trigger job for name {JobName}", jobStatusRequest.JobName);
                            var jobKey = new JobKey(jobStatusRequest.JobName, jobStatusRequest.GroupId);
                            await scheduler.TriggerJob(jobKey);
                            return await Task.Run(() => true);
                        },
                        async () =>
                        {
                            _logger.LogWarning("cannot remove elastic index");
                            return await Task.Run(() => false);
                        });
                return boolReturn;
            },
            async () =>
            {
                _logger.LogWarning("Cannot find scheduler with name {SchedulerName}",
                    _configurationObject.SchedulerName);
                return await Task.Run(() => false);
            });
        return true;
    }

    public async Task<string> TriggerStatusById(TriggerStateRequest triggerStateRequest)
    {
        var schedulerOpt = await SchedulerUtilities.StdSchedulerByName(_configurationObject.SchedulerName);
        return await schedulerOpt.Match(
            async scheduler =>
            {
                var triggerKey = new TriggerKey(triggerStateRequest.TriggerId, triggerStateRequest.GroupId);
                return (await scheduler.GetTriggerState(triggerKey)).ToString();
            },
            async () =>
            {
                _logger.LogWarning("Cannot find scheduler with name {SchedulerName}",
                    _configurationObject.SchedulerName);
                return await Task.Run(() => string.Empty);
            });
    }

    public AdministrationGenericRequest GenericContent()
    {
        var processSubTypes = StaticHelpers.SubtypesOfType<ElasticDocument>();
        var cleanupSubTypes = StaticHelpers.SubtypesOfType<CleanupDocument>();
        AdministrationGenericRequest adminGenModel = _configurationObject;
        adminGenModel.ProcessorConfigurations = _configurationObject
            .Processing
            .Where(d => processSubTypes.Select(st => st.Name).Contains(d.Key))
            .SelectKv((key, value) => Tuple.Create(key, (ProcessorConfiguration) value));
           
        adminGenModel.CleanupConfigurations = _configurationObject
            .Cleanup
            .Where(d => cleanupSubTypes.Select(st => st.Name).Contains(d.Key))
            .SelectKv((key, value) =>
                Tuple.Create(key, (CleanupConfiguration) value));
        return adminGenModel;
    }

    public IAsyncEnumerable<KeyValuePair<string, SchedulerStatistics>> SchedulerContent()
    {
        return _schedulerStatisticsService.SchedulerStatistics();
    }

    private static async IAsyncEnumerable<IndicesStatsResponse> CalculateIndicesStatsResponse(IEnumerable<string> indices, IElasticSearchService elasticSearchService)
    {
        foreach (var index in indices)
            yield return await elasticSearchService.IndexStatistics(index);
    }

    public async Task<IndexStatistic> StatisticsContent()
    {
        async Task<GetIndexResponse> IndicesResponse(string indexName) =>
            await _elasticSearchService.IndicesWithPatternAsync($"{indexName}-*");

        async Task<IEnumerable<string>> KnownIndices(string indexName) =>
            (await IndicesResponse(indexName))
            .Indices
            .Keys
            .Select(index => index.Name);
            
        var knownIndices = await KnownIndices(_configurationObject.IndexName);
        var indexStatsResponses = CalculateIndicesStatsResponse(knownIndices, _elasticSearchService);

        static RunnableStatistic
            ConvertToRunnableStatistic(ProcessingJobStatistic doc, Func<MemoryCacheModel> fn)
        {
            RunnableStatistic ret = doc;
            var cacheEntryOpt = fn.Invoke().CacheEntry();
            if (cacheEntryOpt.IsSome)
                ret.CacheEntry = cacheEntryOpt.ValueUnsafe();
            return ret;
        }

        static IEnumerable<Tuple<ProcessorBase, Func<StatisticModel>>> StatisticUtilities(
            ILoggerFactory loggerFactory, ConfigurationObject configurationObject) =>
            StatisticUtilitiesProxy
                .AsIEnumerable(loggerFactory,
                    new TypedDirectoryPathString(configurationObject.StatisticsDirectory));

        static IEnumerable<Tuple<ProcessorBase, Func<MemoryCacheModel>>> JobStateMemoryCaches(
            ILoggerFactory loggerFactory, IMemoryCache memoryCache) =>
            JobStateMemoryCacheProxy
                .AsIEnumerable(loggerFactory, memoryCache);

        var jobStateMemoryCaches = JobStateMemoryCaches(_loggerFactory, _memoryCache);
        var runtimeStatistic = StatisticUtilities(_loggerFactory, _configurationObject)
            .SelectTuple((processorBase, statisticModel) =>
            {
                return statisticModel
                    .Invoke()
                    .LatestJobStatisticByModel()
                    .Map(doc =>
                    {
                        return jobStateMemoryCaches
                            .Where(d => d.Item1.DerivedModelName == processorBase.DerivedModelName)
                            .Map(jobState => KeyValuePair.Create(processorBase.ShortName,
                                ConvertToRunnableStatistic(doc, jobState.Item2)));
                    });
            })
            .Somes()
            .Flatten()
            .ToDictionary();

        static IAsyncEnumerable<IndexStatisticModel> ConvertToIndexStatisticModel(IAsyncEnumerable<IndicesStatsResponse> responses) =>
            responses.Select(index => (IndexStatisticModel)index);


        static async Task<IndexStatistic> ResponseModel(IAsyncEnumerable<IndicesStatsResponse> indexStatsResponses,
            Dictionary<string, RunnableStatistic> runtimeStatistic)
        {
            var convertedModel = ConvertToIndexStatisticModel(indexStatsResponses);
            // ReSharper disable once PossibleMultipleEnumeration
            var entireDocCount = await CalculateEntireDocCount(convertedModel);
            // ReSharper disable once PossibleMultipleEnumeration
            var entireSizeInBytes = await CalculateEntireIndexSize(convertedModel);
            // ReSharper disable once PossibleMultipleEnumeration
            return new IndexStatistic(convertedModel, runtimeStatistic, entireDocCount, entireSizeInBytes);
        }
        return await ResponseModel(indexStatsResponses, runtimeStatistic);
    }

    private static readonly Func<IAsyncEnumerable<IndexStatisticModel>, ValueTask<long>>
        CalculateEntireDocCount =
            model => model.SumAsync(d => d.DocCount);

    private static readonly Func<IAsyncEnumerable<IndexStatisticModel>, ValueTask<double>>
        CalculateEntireIndexSize = model => model.SumAsync(d => d.SizeInBytes);

    private static readonly Func<IEnumerable<SchedulerTriggerStatisticElement>, Option<JobState>,
        IEnumerable<AdministrationActionTriggerModel>> ConvertTriggerElements =
        (triggerElements, jobStateOpt) => triggerElements.Select(trigger =>
        {
            AdministrationActionTriggerModel triggerElement = trigger;
            triggerElement.JobState = jobStateOpt.GetValueOrDefault(JobState.Undefined);
            return triggerElement;
        });

    private static readonly Func<SchedulerStatistics, Option<JobState>, AdministrationActionSchedulerModel>
        ConvertToActionModel =
            (scheduler, jobStateOpt) => new AdministrationActionSchedulerModel(scheduler.SchedulerName,
                ConvertTriggerElements(scheduler.TriggerElements, jobStateOpt));

    private static readonly Func<MemoryCacheModelProxy, Dictionary<string, JobState>> MemoryCacheStates =
        memoryCacheModelProxy => memoryCacheModelProxy
            .Models()
            .SelectTuple((key, value) => value
                .Invoke()
                .CacheEntry()
                .Match(
                    el => KeyValuePair.Create(key, el.JobState),
                    () => KeyValuePair.Create(key, JobState.Undefined)
                ))
            .ToDictionary();

    private static readonly Func<string, Dictionary<string, JobState>, Option<JobState>> FilterMemoryCacheState =
        (jobName, memoryCacheStates) => memoryCacheStates.TryGetValue(jobName, out var value) ? value : Option<JobState>.None;

    private static readonly Func<SchedulerStatistics, Dictionary<string, JobState>, Option<JobState>>
        CalculateJobState = (schedulerStatisticsArray, memoryCacheStates) => schedulerStatisticsArray
            .TriggerElements
            .Select(keyElement => FilterMemoryCacheState(keyElement.JobName, memoryCacheStates))
            .Somes()
            .First();

    public IAsyncEnumerable<KeyValuePair<string, IEnumerable<AdministrationActionSchedulerModel>>> ActionContent()
    {
        var memoryCacheStates = MemoryCacheStates(_memoryCacheModelProxy);
        var groupedSchedulerModels = _schedulerStatisticsService.SchedulerStatistics()
            .Select(kv =>
            {
                var state = CalculateJobState(kv.Value, memoryCacheStates);
                var model = ConvertToActionModel(kv.Value, state);
                return new KeyValuePair<string, IEnumerable<AdministrationActionSchedulerModel>>(kv.Key,
                    new[] { model });
            });
        return groupedSchedulerModels;
    }
}