using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
        public static IEnumerable<Type> GetSubtypesOfType<TIn>()
            =>
                from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                from assemblyType in domainAssembly.GetTypes()
                where typeof(TIn).IsAssignableFrom(assemblyType)
                where assemblyType.IsSubclassOf(typeof(TIn))
                select assemblyType;



        public static async Task<TypedMd5String> CreateMd5HashString(TypedMd5InputString stringValue)
        {
            return await Task.Run(async () =>
            {
                using var md5 = MD5.Create();
                await using var ms = new MemoryStream();
                await using var wt = new StreamWriter(ms);
                await wt.WriteAsync(stringValue.Value);
                await wt.FlushAsync();
                ms.Position = 0;
                return new TypedMd5String(BitConverter.ToString(await md5.ComputeHashAsync(ms)));
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

        public static TypedCommentString GetStringFromCommentsArray(this IEnumerable<OfficeDocumentComment> commentsArray) =>
            new(string.Join(" ", commentsArray.Select(d => d.Comment)));

        public static TypedSuggestString GenerateTextToSuggest(this TypedCommentString commentString, TypedContentString contentString) =>
            new (Regex.Replace(contentString.Value + " " + commentString.Value, "[^a-zA-ZäöüßÄÖÜ]", " "));

        public static IEnumerable<string> GenerateSearchAsYouTypeArray(this TypedSuggestString suggestedText) =>
            suggestedText
                .Value
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

        public static async Task<TypedMd5String> GetContentHashString(this (List<string> listElementsToHash,
            IEnumerable<OfficeDocumentComment> commentsArray) kv) =>
            await CreateMd5HashString(
                new TypedMd5InputString(BuildHashList(kv.listElementsToHash, kv.commentsArray).JoinString("")));

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

        public static Source<string, NotUsed> UseExcludeFileFilter(this Source<TypedFilePathString, NotUsed> source,
            string excludeFilter)
        {
            return source
                .Where(t => excludeFilter == "" || !t.Value.Contains(excludeFilter))
                .Select(x => x.Value);
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

        [Pure]
        public static Source<TypedFilePathString, NotUsed> CreateSource(this TypedFilePathString scanPath,
            string fileExtension)
        {
            return Source
                .From(Directory.GetFiles(scanPath.Value, fileExtension,
                    SearchOption.AllDirectories).Select(f => new TypedFilePathString(f)));
        }

        public static Task RunIgnore<T>(this Source<T, NotUsed> source, ActorMaterializer actorMaterializer) =>
            source.RunWith(Sink.Ignore<T>(), actorMaterializer);

        public static Source<bool, NotUsed> WriteDocumentsToIndexAsync<TDocument>(this
                Source<IEnumerable<TDocument>, NotUsed> source, SchedulerEntry schedulerEntry,
            IElasticSearchService elasticSearchService, string indexName) where TDocument : ElasticDocument
        {
            return source.SelectAsyncUnordered(schedulerEntry.Parallelism,
                g => elasticSearchService.BulkWriteDocumentsAsync(g, indexName));
        }

        public static Source<Maybe<TDocument>, NotUsed> FilterExistingUnchangedAsync<TDocument>(
            this Source<Maybe<TDocument>, NotUsed> source, SchedulerEntry schedulerEntry,
            ComparerModel comparerModel) where TDocument : ElasticDocument
        {
            return source.SelectAsyncUnordered(schedulerEntry.Parallelism, comparerModel.FilterExistingUnchanged);
        }
    }
}