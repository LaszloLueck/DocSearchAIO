using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Akka;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Util.Internal;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using DocumentFormat.OpenXml;
using Nest;

namespace DocSearchAIO.Scheduler
{
    public static class StaticHelpers
    {
        public static void IfTrue(this bool value, Action action)
        {
            if (value)
                action.Invoke();
        }

        public static void Map<TIn>(this Maybe<TIn> source, Action<TIn> processor)
        {
            if (source.HasValue)
                processor.Invoke(source.Value);
        }

        public static Maybe<TOut> MaybeValue<TOut>(this TOut value)
        {
            return value == null ? Maybe<TOut>.None : Maybe<TOut>.From(value);
        }

        public static IEnumerable<Type> GetSubtypesOfType<TIn>()
            =>
                from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                from assemblyType in domainAssembly.GetTypes()
                where typeof(TIn).IsAssignableFrom(assemblyType)
                where assemblyType.IsSubclassOf(typeof(TIn))
                select assemblyType;

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> source) => source.ToDictionary(d => d.Key, d => d.Value);

        public static void IfTrueFalse<TInputLeft, TInputRight>(this bool value, (TInputLeft, TInputRight) parameters,
            Action<TInputLeft> falseAction,
            Action<TInputRight> trueAction)
        {
            var (inputLeft, inputRight) = parameters;
            if (value)
            {
                trueAction.Invoke(inputRight);
            }
            else
            {
                falseAction.Invoke(inputLeft);
            }
        }

        public static TOut Match<TOut, TKey, TValue>(this Maybe<KeyValuePair<TKey, TValue>> kvOpt,
            Func<TKey, TValue, TOut> some, Func<TOut> none) =>
            kvOpt.HasValue ? some.Invoke(kvOpt.Value.Key, kvOpt.Value.Value) : none.Invoke();
        
        public static void Match<TKey, TValue>(this Maybe<KeyValuePair<TKey, TValue>> maybe, Action<TKey, TValue> some,
            Action none)
        {
            if (maybe.HasValue)
            {
                some.Invoke(maybe.Value.Key, maybe.Value.Value);
            }
            else
            {
                none.Invoke();
            }
        }

        public static void ForEach<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> kvList,
            Action<TKey, TValue> action) => kvList.ForEach(kv => action.Invoke(kv.Key, kv.Value));

        public static TOut IfTrueFalse<TOut>(this bool value,
            Func<TOut> falseAction,
            Func<TOut> trueAction)
        {
            return value ? trueAction.Invoke() : falseAction.Invoke();
        }

        public static void DictionaryKeyExistsAction<TDicKey, TDicValue>(
            this Dictionary<TDicKey, TDicValue> source, TDicKey comparer,
            Action<KeyValuePair<TDicKey, TDicValue>> action)
        {
            if (source.ContainsKey(comparer))
                action.Invoke(new KeyValuePair<TDicKey, TDicValue>(comparer, source[comparer]));
        }

        public static TOut ValueOr<TOut>(this TOut value, TOut alternative) => value is null ? alternative is null ? default : alternative : value;

            public static Source<IEnumerable<TSource>, TMat> WithMaybeFilter<TSource, TMat>(
            this Source<IEnumerable<Maybe<TSource>>, TMat> source) => source.Select(Values);

        public static IEnumerable<TSource> Values<TSource>(this IEnumerable<Maybe<TSource>> source) =>
            source
                .Where(filtered => filtered.HasValue)
                .Select(selected => selected.Value);

        public static async Task<string> CreateMd5HashString(string stringValue)
        {
            return await Task.Run(async () =>
            {
                using var md5 = MD5.Create();
                await using var ms = new MemoryStream();
                await using var wt = new StreamWriter(ms);
                await wt.WriteAsync(stringValue);
                await wt.FlushAsync();
                ms.Position = 0;
                return BitConverter.ToString(await md5.ComputeHashAsync(ms));
            });
        }

        public static readonly Func<string, string[]> KeywordsList = keywords =>
            keywords.Length == 0 ? Array.Empty<string>() : keywords.Split(",");

        public static Source<TSource, TMat> CountEntireDocs<TSource, TMat, TModel>(this Source<TSource, TMat> source,
            StatisticUtilities<TModel> statisticUtilities) where TModel : StatisticModel
        {
            return source.Select(t =>
            {
                statisticUtilities.AddToEntireDocuments();
                return t;
            });
        }

        public static string JoinString(this IEnumerable<string> source, string separator) =>
            string.Join(separator, source);

        public static Source<IEnumerable<TSource>, TMat> CountFilteredDocs<TSource, TMat, TModel>(
            this Source<IEnumerable<TSource>, TMat> source,
            StatisticUtilities<TModel> statisticUtilities) where TModel : StatisticModel
        {
            return source.Select(e =>
            {
                var cntArr = e.ToArray();
                statisticUtilities.AddToChangedDocuments(cntArr.Length);
                return cntArr.AsEnumerable();
            });
        }

        public static string GetStringFromCommentsArray(this IEnumerable<OfficeDocumentComment> commentsArray) =>
            string.Join(" ", commentsArray.Select(d => d.Comment));

        public static string GenerateTextToSuggest(this string commentString, string contentString) =>
            Regex.Replace(contentString + " " + commentString, "[^a-zA-ZäöüßÄÖÜ]", " ");

        public static IEnumerable<string> GenerateSearchAsYouTypeArray(this string suggestedText) =>
            suggestedText
                .ToLower()
                .Split(" ")
                .Distinct()
                .Where(d => !string.IsNullOrWhiteSpace(d) || !string.IsNullOrEmpty(d))
                .Where(d => d.Length > 2);

        public static string GetContentString(this IEnumerable<OpenXmlElement> openXmlElementList)
        {
            var sw = new StringBuilder(4096);
            ExtractTextFromElement(openXmlElementList, sw);
            var s = sw.ToString();
            return s;
        }

        public static CompletionField WrapCompletionField(this
            IEnumerable<string> searchAsYouTypeContent) =>
            new() {Input = searchAsYouTypeContent};

        private static IEnumerable<string[]> GetCommentsString(
            IEnumerable<OfficeDocumentComment> commentsArray) =>
            commentsArray
                .Where(l => l.Comment is not null)
                .Select(l => l.Comment.Split(" "))
                .Distinct()
                .ToList();

        private static IEnumerable<string> BuildHashList(IEnumerable<string> listElementsToHash,
            IEnumerable<OfficeDocumentComment> commentsArray) =>
            listElementsToHash
                .Concat(
                    GetCommentsString(commentsArray)
                        .Where(d => d.Any())
                        .SelectMany(k => k).Distinct());

        public static async Task<string> GetContentHashString(this (List<string> listElementsToHash,
            IEnumerable<OfficeDocumentComment> commentsArray) kv) =>
            await CreateMd5HashString(
                BuildHashList(kv.listElementsToHash, kv.commentsArray).JoinString(""));

        public static List<string> GetListElementsToHash(string category, DateTime created,
            string contentString, string creator, string description, string identifier,
            string keywords, string language, DateTime modified, string revision,
            string subject, string title, string version, string contentStatus,
            string contentType, DateTime lastPrinted, string lastModifiedBy) =>
            new()
            {
                category,
                created.ToString(CultureInfo.CurrentCulture),
                contentString,
                creator,
                description,
                identifier,
                keywords,
                language,
                modified.ToString(CultureInfo.CurrentCulture),
                revision,
                subject,
                title,
                version,
                contentStatus,
                contentType,
                lastPrinted.ToString(CultureInfo.CurrentCulture),
                lastModifiedBy
            };

        private static readonly Action<IEnumerable<OpenXmlElement>, StringBuilder> ExtractTextFromElement =
            (list, sb) =>
            {
                list
                    .ForEach(element =>
                    {
                        switch (element.LocalName)
                        {
                            case "t" when !element.ChildElements.Any():
                                if (element.InnerText.Any())
                                    sb.AppendLine(element.InnerText);
                                break;
                            case "p":
                                ExtractTextFromElement(element.ChildElements, sb);
                                sb.AppendLine();
                                break;
                            case "br":
                                sb.AppendLine();
                                break;
                            default:
                                ExtractTextFromElement(element.ChildElements, sb);
                                break;
                        }
                    });
            };

        public static Source<string, NotUsed> UseExcludeFileFilter(this Source<GenericSourceFilePath, NotUsed> source,
            string excludeFilter)
        {
            return source
                .Select(p => p.Value)
                .Where(t => excludeFilter == "" || !t.Contains(excludeFilter));
        }

        public static string ReplaceSpecialStrings(this string input, IList<(string, string)> list)
        {
            while (true)
            {
                if (!list.Any()) return input;

                input = Regex.Replace(input, list[0].Item1, list[0].Item2);
                list.RemoveAt(0);
            }
        }

        public static Source<GenericSourceFilePath, NotUsed> CreateSource(this GenericSourceFilePath scanPath,
            string fileExtension)
        {
            return Source
                .From(Directory.GetFiles(scanPath.Value, fileExtension,
                    SearchOption.AllDirectories).Select(f => new GenericSourceFilePath(f)));
        }

        public static Task RunIgnore(this Source<bool, NotUsed> source, ActorMaterializer actorMaterializer) =>
            source.RunWith(Sink.Ignore<bool>(), actorMaterializer);

        public static Source<bool, NotUsed> WriteDocumentsToIndexAsync<TDocument>(this
                Source<IEnumerable<TDocument>, NotUsed> source, SchedulerEntry schedulerEntry,
            IElasticSearchService elasticSearchService, string indexName) where TDocument : ElasticDocument
        {
            return source.SelectAsync(schedulerEntry.Parallelism,
                g => elasticSearchService.BulkWriteDocumentsAsync(g, indexName));
        }

        public static Source<Maybe<TDocument>, NotUsed> FilterExistingUnchangedAsync<TDocument>(
            this Source<Maybe<TDocument>, NotUsed> source, SchedulerEntry schedulerEntry,
            ComparerModel comparerModel) where TDocument : ElasticDocument
        {
            return source.SelectAsync(schedulerEntry.Parallelism, comparerModel.FilterExistingUnchanged);
        }
    }
}