using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.Services;
using DocSearchAIO.Utilities;
using DocumentFormat.OpenXml;
using Nest;

namespace DocSearchAIO.Scheduler
{
    public static class StaticHelpers
    {
        [Pure]
        public static IEnumerable<Type> SubtypesOfType<TIn>()
            =>
                from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                from assemblyType in domainAssembly.GetTypes()
                where typeof(TIn).IsAssignableFrom(assemblyType)
                where assemblyType.IsSubclassOf(typeof(TIn))
                select assemblyType;

        public static readonly Func<TypedMd5InputString, Task<TypedMd5String>> CreateMd5HashString =
            async stringValue =>
            {
                    using var md5 = MD5.Create();
                    await using var ms = new MemoryStream();
                    await using var wt = new StreamWriter(ms);
                    await wt.WriteAsync(stringValue.Value);
                    await wt.FlushAsync();
                    ms.Position = 0;
                    return new TypedMd5String(BitConverter.ToString(await md5.ComputeHashAsync(ms)));
            };

        public static readonly Func<ConfigurationObject, string[], string, bool>
            GetIndexKeyExpressionFromConfiguration =
                (configurationObject, indexNames, configurationKey) => configurationObject.Processing.ContainsKey(configurationKey) && indexNames.Contains(
                    $"{configurationObject.IndexName}-{configurationObject.Processing[configurationKey].IndexSuffix}");

        public static readonly Func<string, string[]> KeywordsList = keywords =>
            keywords.Length == 0 ? Array.Empty<string>() : keywords.Split(",");

        [Pure]
        public static Source<TSource, TMat> CountEntireDocs<TSource, TMat, TModel>(this Source<TSource, TMat> source,
            StatisticUtilities<TModel> statisticUtilities) where TModel : StatisticModel
        {
            return source.Select(t =>
            {
                statisticUtilities.AddToEntireDocuments();
                return t;
            });
        }

        [Pure]
        public static string Join([NotNull] this IEnumerable<string> source, [NotNull] string separator) =>
            string.Join(separator, source);

        [Pure]
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

        [Pure]
        public static TypedCommentString StringFromCommentsArray(
            this IEnumerable<OfficeDocumentComment> commentsArray) =>
            new(commentsArray.Select(d => d.Comment).Join(" "));

        [Pure]
        public static TypedSuggestString GenerateTextToSuggest(this TypedCommentString commentString,
            TypedContentString contentString) =>
            new(Regex.Replace(contentString.Value + " " + commentString.Value, "[^a-zA-ZäöüßÄÖÜ]", " "));

        [Pure]
        public static IEnumerable<string> GenerateSearchAsYouTypeArray(this TypedSuggestString suggestedText) =>
            suggestedText
                .Value
                .ToLower()
                .Split(" ")
                .Distinct()
                .Where(d => !string.IsNullOrWhiteSpace(d) || !string.IsNullOrEmpty(d))
                .Where(d => d.Length > 2);


        [Pure]
        public static CompletionField WrapCompletionField(this
            IEnumerable<string> searchAsYouTypeContent) =>
            new() {Input = searchAsYouTypeContent};

        private static readonly Func<IEnumerable<OfficeDocumentComment>, IEnumerable<string[]>> CommentsString =
            commentsArray =>
            {
                return commentsArray
                    .Select(l => l.Comment.Split(" "))
                    .Distinct()
                    .ToList();
            };
        
        private static readonly Func<IEnumerable<string>, IEnumerable<OfficeDocumentComment>, IEnumerable<string>>
            BuildHashList =
                (listElementsToHash, commentsArray) =>
                {
                    return listElementsToHash
                        .Concat(
                            CommentsString(commentsArray)
                                .Where(d => d.Any())
                                .SelectMany(k => k).Distinct()
                        );
                };

        [Pure]
        public static async Task<TypedMd5String> ContentHashString(this (List<string> listElementsToHash,
            IEnumerable<OfficeDocumentComment> commentsArray) kv) =>
            await CreateMd5HashString(
                new TypedMd5InputString(BuildHashList(kv.listElementsToHash, kv.commentsArray).Join("")));

        [Pure]
        public static List<string> ListElementsToHash(string category, DateTime created,
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

        [Pure]
        public static string ContentString(this IEnumerable<OpenXmlElement> openXmlElementList)
        {
            return openXmlElementList
                .Select(TextFromParagraph)
                .Join(" ");
        }

        [Pure]
        private static string TextFromParagraph(this OpenXmlElement paragraph)
        {
            var sb = new StringBuilder();
            ExtractTextFromElement(paragraph.ChildElements, sb);
            return sb.ToString();
        }


        private static void ExtractTextFromElement([DisallowNull] IEnumerable<OpenXmlElement> list,
            [DisallowNull] StringBuilder sb)
        {
            list.ForEach(element =>
            {
                switch (element)
                {
                    case DocumentFormat.OpenXml.Wordprocessing.Paragraph p when p.InnerText.Any():
                        sb.Append(' ');
                        ExtractTextFromElement(element.ChildElements, sb);
                        sb.Append(' ');
                        break;
                    case DocumentFormat.OpenXml.Wordprocessing.Text {HasChildren: false} wText:
                        if (wText.Text.Any())
                            sb.Append(wText.Text);
                        break;
                    case DocumentFormat.OpenXml.Spreadsheet.Text {HasChildren: false} sText:
                        if (sText.Text.Any())
                            sb.Append(sText.Text);
                        break;
                    case DocumentFormat.OpenXml.Drawing.Text {HasChildren: false} dText:
                        if (dText.Text.Any())
                            sb.Append(dText.Text);
                        break;
                    case DocumentFormat.OpenXml.Presentation.TextBody {HasChildren: false} tText:
                        if (tText.TextFromParagraph().Any())
                            sb.Append(' ' + tText.TextFromParagraph() + ' ');
                        break;
                    case DocumentFormat.OpenXml.Drawing.Paragraph drawParagraph when drawParagraph.InnerText.Any():
                        sb.Append(' ');
                        ExtractTextFromElement(drawParagraph.ChildElements, sb);
                        sb.Append(' ');
                        break;
                    case DocumentFormat.OpenXml.Presentation.Text {HasChildren: false} pText:
                        if (pText.Text.Any())
                            sb.Append(pText.Text);
                        break;
                    case DocumentFormat.OpenXml.Wordprocessing.FieldChar
                        {FieldCharType: {Value: DocumentFormat.OpenXml.Wordprocessing.FieldCharValues.Separate}}:
                        sb.Append(' ');
                        break;
                    case DocumentFormat.OpenXml.Wordprocessing.Break:
                        sb.Append(Environment.NewLine);
                        break;
                    default:
                        if (element.InnerText.Any())
                            ExtractTextFromElement(element.ChildElements, sb);
                        break;
                }
            });
        }

        [Pure]
        public static Source<string, NotUsed> UseExcludeFileFilter(this Source<TypedFilePathString, NotUsed> source,
            string excludeFilter)
        {
            return source
                .Where(t => excludeFilter == string.Empty || !t.Value.Contains(excludeFilter))
                .Select(x => x.Value);
        }

        [Pure]
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

        [Pure]
        public static Task RunIgnore<T>(this Source<T, NotUsed> source, ActorMaterializer actorMaterializer) =>
            source.RunWith(Sink.Ignore<T>(), actorMaterializer);

        [Pure]
        public static Source<bool, NotUsed> WriteDocumentsToIndexAsync<TDocument>(this
                Source<IEnumerable<TDocument>, NotUsed> source, SchedulerEntry schedulerEntry,
            IElasticSearchService elasticSearchService, string indexName) where TDocument : ElasticDocument
        {
            return source.SelectAsyncUnordered(schedulerEntry.Parallelism,
                g => elasticSearchService.BulkWriteDocumentsAsync(g, indexName));
        }

        [Pure]
        public static Source<Maybe<TDocument>, NotUsed> FilterExistingUnchangedAsync<TDocument>(
            this Source<Maybe<TDocument>, NotUsed> source, SchedulerEntry schedulerEntry,
            ComparerModel comparerModel) where TDocument : ElasticDocument
        {
            return source.SelectAsyncUnordered(schedulerEntry.Parallelism, comparerModel.FilterExistingUnchanged);
        }
    }
}