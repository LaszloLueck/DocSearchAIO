using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using DocSearchAIO.Utilities;
using Microsoft.Extensions.Logging;

namespace DocSearchAIO.Classes
{
    public abstract class ComparerModel
    {
        private readonly ILogger? _logger;

        protected abstract string DerivedModelName { get; }

        private readonly string _comparerDirectory;

        private string ComparerFileName => $"cmp_{DerivedModelName}.cmp";

        public string ComparerFilePath => $"{_comparerDirectory}/{ComparerFileName}";

        private readonly ConcurrentDictionary<string, ComparerObject> _comparerObjects = new();

        protected ComparerModel(ILoggerFactory loggerFactory, string comparerDirectory)
        {
            _logger = loggerFactory.CreateLogger<ComparerModel>();
            _comparerDirectory = comparerDirectory;
            CheckAndCreateComparerDirectory();
            _comparerObjects = ComparerHelper.FillConcurrentDictionary(ComparerFilePath);
        }

        protected ComparerModel(string comparerDirectory)
        {
            _comparerDirectory = comparerDirectory;
        }

        public void CleanDictionaryAndRemoveComparerFile()
        {
            _logger.LogInformation("cleanup comparer dictionary before removing the file");
            _comparerObjects.Clear();
            RemoveComparerFile();
            CheckAndCreateComparerDirectory();
        }

        public void RemoveComparerFile()
        {
            _logger.LogInformation("remove comparer file {GetComparerFilePath} for key {DerivedModelName}",
                ComparerFilePath, DerivedModelName);
            ComparerHelper.RemoveComparerFile(ComparerFilePath);
        }

        public async Task WriteAllLinesAsync()
        {
            _logger.LogInformation("write new comparer file in {ComparerDirectory}", _comparerDirectory);
            var sw = Stopwatch.StartNew();
            try
            {
                await ComparerHelper.WriteAllLinesAsync(_comparerObjects, ComparerFilePath);
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
            if (!ComparerHelper.CheckIfDirectoryExists(_comparerDirectory))
                ComparerHelper.CreateDirectory(_comparerDirectory);
            _logger.LogInformation("check if comparer file {GetComparerFilePath} exists", ComparerFilePath);
            if (!ComparerHelper.CheckIfFileExists(ComparerFilePath))
                ComparerHelper.CreateComparerFile(ComparerFilePath);
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
                                var innerDoc = new ComparerObject(pathHash, contentHash, originalFilePath);
                                _comparerObjects.AddOrUpdate(pathHash, innerDoc, (_, _) => innerDoc);
                                return Maybe<TModel>.From(doc);
                            },
                            () =>
                            {
                                if (comparerObject == null) return Maybe<TModel>.None;

                                if (comparerObject.DocumentHash == contentHash)
                                    return Maybe<TModel>.None;
                                var comparerObjectCopy = new ComparerObject(comparerObject.PathHash, contentHash,
                                    comparerObject.OriginalPath);
                                _comparerObjects.AddOrUpdate(pathHash, comparerObjectCopy,
                                    (_, _) => comparerObjectCopy);
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

    public class ComparerModelMsg : ComparerModel
    {
        protected override string DerivedModelName => GetType().Name;

        public ComparerModelMsg(string comparerDirectory) : base(comparerDirectory)
        {
        }

        public ComparerModelMsg(ILoggerFactory loggerFactory, string comparerDirectory) : base(loggerFactory,
            comparerDirectory)
        {
        }
        
    }

    public class ComparerModelEml : ComparerModel
    {
        public ComparerModelEml(ILoggerFactory loggerFactory, string comparerDirectory) : base(loggerFactory, comparerDirectory)
        {
        }

        public ComparerModelEml(string comparerDirectory) : base(comparerDirectory)
        {
        }

        protected override string DerivedModelName => GetType().Name;
    }
}