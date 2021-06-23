using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using DocSearchAIO.Scheduler;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Classes
{
    public abstract class ComparerModel
    {
        private readonly ILogger _logger;

        protected abstract string DerivedModelName { get; }

        private readonly string _comparerDirectory;

        private string GetComparerFileName => $"cmp_{DerivedModelName}.cmp";

        private string GetComparerFilePath => $"{_comparerDirectory}/{GetComparerFileName}";

        private ConcurrentDictionary<string, ComparerObject> _comparerObjects;

        protected ComparerModel()
        {
        }

        protected ComparerModel(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ComparerModel>();
        }

        protected ComparerModel(ILoggerFactory loggerFactory, string comparerDirectory)
        {
            _logger = loggerFactory.CreateLogger<ComparerModel>();
            _comparerDirectory = comparerDirectory;
            _comparerObjects = new ConcurrentDictionary<string, ComparerObject>();
            CheckAndCreateComparerDirectory();
            FillConcurrentDictionary();
        }

        public void CleanDictionaryAndRemoveComparerFile()
        {
            _logger.LogInformation("cleanup comparer dictionary before removing the file");
            _comparerObjects?.Clear();
            RemoveComparerFile();
        }

        public void RemoveComparerFile()
        {
            _logger.LogInformation($"remove comparer file {GetComparerFilePath} for key {DerivedModelName}");
            File.Delete(GetComparerFilePath);
        }

        public async Task WriteAllLinesAsync()
        {
            _logger.LogInformation($"write new comparer file in {_comparerDirectory}");
            var sw = Stopwatch.StartNew();
            try
            {
                await File.WriteAllLinesAsync(GetComparerFilePath,
                    _comparerObjects.Select(tpl =>
                        $"{tpl.Value.DocumentHash};{tpl.Value.PathHash};{tpl.Value.OriginalPath}"));
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation($"WriteAllLinesAsync needs {sw.ElapsedMilliseconds} ms");
            }
        }

        private void CheckAndCreateComparerDirectory()
        {
            _logger.LogInformation($"check if directory {_comparerDirectory} exists");
            if (!Directory.Exists(_comparerDirectory))
                Directory.CreateDirectory(_comparerDirectory);
            _logger.LogInformation($"check if comparer file {GetComparerFilePath} exists");
            if (!File.Exists(GetComparerFilePath))
                File.Create(GetComparerFilePath).Dispose();
        }

        private void FillConcurrentDictionary()
        {
            _logger.LogInformation($"fill comparer dictionary for key {DerivedModelName}");
            var sw = Stopwatch.StartNew();
            try
            {
                var bulk = File
                    .ReadAllLines(GetComparerFilePath)
                    .AsParallel()
                    .WithDegreeOfParallelism(10)
                    .Select(line =>
                    {
                        var spl = line.Split(";");
                        if (spl.Length != 3) return Maybe<KeyValuePair<string, ComparerObject>>.None;
                        var cpo = new ComparerObject
                            {DocumentHash = spl[0], PathHash = spl[1], OriginalPath = spl[2]};
                        return Maybe<KeyValuePair<string, ComparerObject>>.From(new KeyValuePair<string,ComparerObject>(cpo.PathHash, cpo));
                    })
                    .Values();
                _comparerObjects = new ConcurrentDictionary<string, ComparerObject>(bulk);
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation($"FillConcurrentDictionary needs {sw.ElapsedMilliseconds} ms for {_comparerObjects.Count} entries");
            }
        }

        public async Task<Maybe<TModel>> FilterExistingUnchanged<TModel>(Maybe<TModel> document) where TModel : ElasticDocument
        {
            return await Task.Run(() =>
            {
                var opt = document.Bind(doc =>
                {
                    var contentHash = doc.ContentHash;
                    var pathHash = doc.Id;
                    var originalFilePath = doc.OriginalFilePath;
                    return _comparerObjects
                        .TryGetValue(pathHash, out var comparerObject)
                        .IfTrueFalse(
                            () =>
                            {
                                var innerDoc = new ComparerObject
                                {
                                    DocumentHash = contentHash,
                                    PathHash = pathHash,
                                    OriginalPath = originalFilePath
                                };
                                _comparerObjects.AddOrUpdate(pathHash, innerDoc, (_, _) => innerDoc);
                                return Maybe<TModel>.From(doc);
                            },
                            () =>
                            {
                                if (comparerObject == null) return Maybe<TModel>.None;

                                if (comparerObject.DocumentHash == contentHash)
                                    return Maybe<TModel>.None;

                                comparerObject.DocumentHash = contentHash;
                                _comparerObjects.AddOrUpdate(pathHash, comparerObject,
                                    (_, _) => comparerObject);
                                return Maybe<TModel>.From(doc);
                            });
                });
                return opt;
            });
        }
    }

    public class ComparerModelWord : ComparerModel
    {
        protected override string DerivedModelName => GetType().Name;


        public ComparerModelWord(ILoggerFactory loggerFactory, string comparerDirectory) : base(loggerFactory,
            comparerDirectory)
        {
            
        }

        public ComparerModelWord(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        public ComparerModelWord()
        {
        }
    }

    public class ComparerModelPowerpoint : ComparerModel
    {
        protected override string DerivedModelName => GetType().Name;

        public ComparerModelPowerpoint(ILoggerFactory loggerFactory, string comparerDirectory) : base(loggerFactory, comparerDirectory)
        {
        }

        public ComparerModelPowerpoint(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        public ComparerModelPowerpoint()
        {
        }
    }

    public class ComparerModelPdf : ComparerModel
    {
        protected override string DerivedModelName => GetType().Name;

        public ComparerModelPdf(ILoggerFactory loggerFactory, string comparerDirectory) : base(loggerFactory, comparerDirectory)
        {
        }

        public ComparerModelPdf(ILoggerFactory loggerFactory) : base(loggerFactory)
        {
        }

        public ComparerModelPdf()
        {
        }
    }
}