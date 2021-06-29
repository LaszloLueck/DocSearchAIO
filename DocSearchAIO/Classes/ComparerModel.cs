using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using DocSearchAIO.Scheduler;
using DocSearchAIO.Utilities;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Classes
{
    public abstract class ComparerModel
    {
        private readonly ILogger _logger;

        protected abstract string DerivedModelName { get; }

        private readonly string _comparerDirectory;

        private string GetComparerFileName => $"cmp_{DerivedModelName}.cmp";

        public string GetComparerFilePath => $"{_comparerDirectory}/{GetComparerFileName}";

        private readonly ConcurrentDictionary<string, ComparerObject> _comparerObjects;

        protected ComparerModel(ILoggerFactory loggerFactory, string comparerDirectory)
        {
            _logger = loggerFactory.CreateLogger<ComparerModel>();
            _comparerDirectory = comparerDirectory;
            _comparerObjects = ComparerHelper.FillConcurrentDictionary(GetComparerFilePath);
            CheckAndCreateComparerDirectory();
        }

        protected ComparerModel(string comparerDirectory)
        {
            _comparerDirectory = comparerDirectory;
        }

        public void CleanDictionaryAndRemoveComparerFile()
        {
            _logger.LogInformation("cleanup comparer dictionary before removing the file");
            _comparerObjects?.Clear();
            RemoveComparerFile();
        }

        public void RemoveComparerFile()
        {
            _logger.LogInformation("remove comparer file {GetComparerFilePath} for key {DerivedModelName}",
                GetComparerFilePath, DerivedModelName);
            ComparerHelper.RemoveComparerFile(GetComparerFilePath);
        }

        public async Task WriteAllLinesAsync()
        {
            _logger.LogInformation("write new comparer file in {ComparerDirectory}", _comparerDirectory);
            var sw = Stopwatch.StartNew();
            try
            {
                await ComparerHelper.WriteAllLinesAsync(_comparerObjects, GetComparerFilePath);
            }
            finally
            {
                sw.Stop();
                _logger.LogInformation("WriteAllLinesAsync needs {ElapsedTimeMs} ms", sw.ElapsedMilliseconds);
            }
        }

        private void CheckAndCreateComparerDirectory()
        {
            _logger.LogInformation("check if directory {ComparerDirectory} exists", _comparerDirectory);
            if (!Directory.Exists(_comparerDirectory))
                Directory.CreateDirectory(_comparerDirectory);
            _logger.LogInformation("check if comparer file {GetComparerFilePath} exists", GetComparerFilePath);
            if (!File.Exists(GetComparerFilePath))
                File.Create(GetComparerFilePath).Dispose();
        }

        public async Task<Maybe<TModel>> FilterExistingUnchanged<TModel>(Maybe<TModel> document)
            where TModel : ElasticDocument
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

        public ComparerModelWord(string comparerDirectory) : base(comparerDirectory)
        {
        }

        public ComparerModelWord(ILoggerFactory loggerFactory, string comparerDirectory) : base(loggerFactory,
            comparerDirectory)
        {
        }
    }

    public class ComparerModelPowerpoint : ComparerModel
    {
        protected override string DerivedModelName => GetType().Name;

        public ComparerModelPowerpoint(string comparerDirectory) : base(comparerDirectory)
        {
        }

        public ComparerModelPowerpoint(ILoggerFactory loggerFactory, string comparerDirectory) : base(loggerFactory,
            comparerDirectory)
        {
        }
    }

    public class ComparerModelPdf : ComparerModel
    {
        protected override string DerivedModelName => GetType().Name;

        public ComparerModelPdf(string comparerDirectory) : base(comparerDirectory)
        {
        }

        public ComparerModelPdf(ILoggerFactory loggerFactory, string comparerDirectory) : base(loggerFactory,
            comparerDirectory)
        {
        }
    }

    public class ComparerModelExcel : ComparerModel
    {
        protected override string DerivedModelName => GetType().Name;

        public ComparerModelExcel(string comparerDirectory) : base(comparerDirectory)
        {
        }

        public ComparerModelExcel(ILoggerFactory loggerFactory, string comparerDirectory) : base(loggerFactory,
            comparerDirectory)
        {
        }
    }
}