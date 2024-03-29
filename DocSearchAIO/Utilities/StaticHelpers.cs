using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text.RegularExpressions;
using Akka;
using Akka.Streams;
using Akka.Streams.Dsl;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.TOs;
using DocSearchAIO.Services;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using LanguageExt;
using Nest;
using Array = System.Array;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace DocSearchAIO.Utilities;

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

    public static readonly Func<TypedHashedInputString, Task<TypedHashedString>> CreateHashString =
        async (stringValue) =>
        {
            var res1 = await EncryptionService.ComputeHashAsync(stringValue);
            var toHash = await EncryptionService.ConvertToStringFromByteArray(res1);
            return TypedHashedString.New(toHash);
        };

    private static readonly Func<ConfigurationObject, string[], string, bool>
        IndexKeyExpressionFromConfiguration =
            (configurationObject, indexNames, configurationKey) =>
                configurationObject.Processing.ContainsKey(configurationKey) && indexNames.Contains(
                    $"{configurationObject.IndexName}-{configurationObject.Processing[configurationKey].IndexSuffix}");

    [Pure]
    public static bool IndexKeyExpression<T>(ConfigurationObject configurationObject, string[] enumerable,
        bool filter = true) where T : ElasticDocument
    {
        return IndexKeyExpressionFromConfiguration(configurationObject, enumerable, typeof(T).Name) && filter;
    }

    public static readonly Func<Type, ConfigurationObject, string[], bool, bool> TypedIndexKeyExistsAndFilter =
        (type, configurationObject, enumerable, filter) =>
            IndexKeyExpressionFromConfiguration(configurationObject, enumerable, type.Name) && filter;

    public static readonly Func<Type, ConfigurationObject, string> IndexNameByType = (type, configurationObject) =>
        $"{configurationObject.IndexName}-{configurationObject.Processing[type.Name].IndexSuffix}";

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
    public static string Join(this IEnumerable<string> source, string separator) =>
        string.Join(separator, source);

    [Pure]
    public static string Concat(this IEnumerable<string> source) => string.Concat(source);


    [Pure]
    public static Source<IEnumerable<TSource>, TMat> CountFilteredDocs<TSource, TMat, TModel>(
        this Source<IEnumerable<TSource>, TMat> source,
        StatisticUtilities<TModel> statisticUtilities) where TModel : StatisticModel
    {
        return source.Select(e =>
        {
            var enumerable = e as TSource[] ?? e.ToArray();
            statisticUtilities.AddToChangedDocuments(enumerable.Length);
            return (IEnumerable<TSource>)enumerable;
        });
    }

    [Pure]
    public static TypedCommentString StringFromCommentsArray(
        this IEnumerable<OfficeDocumentComment> commentsArray) =>
        TypedCommentString.New(commentsArray.Map(d => d.Comment).Join(" "));

    private const string RegexPattern = @"[^a-zA-Z äöüÄÖÜß]";

    [Pure]
    public static async Task<TypedSuggestString> GenerateTextToSuggestAsync(this TypedCommentString commentString,
        TypedContentString contentString)
    {
        var allowed = await Task.Run(() =>
            Regex.Replace($"{commentString.Value} {contentString.Value}", RegexPattern, string.Empty));

        return TypedSuggestString.New(allowed);
    }

    [Pure]
    public static IEnumerable<string> GenerateSearchAsYouTypeArray(this TypedSuggestString suggestedText) =>
        suggestedText
            .Value
            .ToLower()
            .Split(" ")
            .Distinct()
            .Filter(d => !string.IsNullOrWhiteSpace(d) || !string.IsNullOrEmpty(d))
            .Filter(d => d.Length > 2);


    [Pure]
    public static CompletionField WrapCompletionField(this
        IEnumerable<string> searchAsYouTypeContent) =>
        new() { Input = searchAsYouTypeContent };

    private static readonly Func<IEnumerable<OfficeDocumentComment>, IEnumerable<string[]>> CommentsString =
        commentsArray =>
        {
            return commentsArray
                .Map(l => l.Comment.Split(" "))
                .Distinct();
        };

    private static readonly Func<IEnumerable<string>, IEnumerable<OfficeDocumentComment>, IEnumerable<string>>
        BuildHashList =
            (listElementsToHash, commentsArray) =>
            {
                return listElementsToHash
                    .Concat(
                        CommentsString(commentsArray)
                            .Filter(d => d.Any())
                            .Flatten()
                            .Distinct()
                    );
            };

    [Pure]
    public static async Task<TypedHashedString> ContentHashStringAsync(this (List<string> listElementsToHash,
        IEnumerable<OfficeDocumentComment> commentsArray) kv) =>
        await CreateHashString(
            TypedHashedInputString.New(
                BuildHashList(kv.listElementsToHash, kv.commentsArray).Concat()
            )
        );


    [Pure]
    public static List<string> ListElementsToHash(ElementsToHash toHash) =>
        new()
        {
            toHash.Category,
            toHash.Created.ToString(CultureInfo.CurrentCulture),
            toHash.ContentString,
            toHash.Creator,
            toHash.Description,
            toHash.Identifier,
            toHash.Keywords,
            toHash.Language,
            toHash.Modified.ToString(CultureInfo.CurrentCulture),
            toHash.Revision,
            toHash.Subject,
            toHash.Title,
            toHash.Version,
            toHash.ContentStatus,
            toHash.ContentType,
            toHash.LastPrinted.ToString(CultureInfo.CurrentCulture),
            toHash.LastModifiedBy
        };

    [Pure]
    public static async Task<string> ContentStringAsync(this IEnumerable<OpenXmlElement> openXmlElementList)
    {
        var resultTasks = await openXmlElementList.Map(TextFromParagraphAsync).SequenceSerial();
        return resultTasks.Join(" ");
    }

    [Pure]
    private static async Task<string> TextFromParagraphAsync(OpenXmlElement paragraph)
    {
        var resultTask = await (await ExtractTextFromElementAsync(paragraph.ChildElements)).SequenceSerial();
        return resultTask.Join(" ");
    }


    [Pure]
    private static async Task<IEnumerable<Task<string>>> ExtractTextFromElementAsync(IEnumerable<OpenXmlElement> list)
    {
        return await Task.Run(() =>
            list.Map(async element =>
            {
                return element switch
                {
                    Paragraph p when p.InnerText.Any() => $" {(await TextFromParagraphAsync(element))} ",
                    Text { HasChildren: false } t when t.Text.Any() => t.Text,
                    DocumentFormat.OpenXml.Spreadsheet.Text { HasChildren: false } t when t.Text.Any() => t.Text,
                    DocumentFormat.OpenXml.Drawing.Text { HasChildren: false } t when t.Text.Any() => t.Text,
                    TextBody { HasChildren: false } t => $" {await TextFromParagraphAsync(t)} ",
                    DocumentFormat.OpenXml.Drawing.Paragraph d when d.InnerText.Any() =>
                        $" {await TextFromParagraphAsync(d)} ",
                    DocumentFormat.OpenXml.Presentation.Text { HasChildren: false } t when t.Text.Any() => t.Text,
                    FieldChar { FieldCharType.Value: FieldCharValues.Separate } => " ",
                    Break => " ",
                    _ when element.InnerText.Any() => await TextFromParagraphAsync(element),
                    _ => ""
                };
            })
        );
    }

    [Pure]
    public static Source<TypedFilePathString, NotUsed> CreateSource(this IEnumerable<TypedFilePathString> paths)
    {
        return Source.From(paths);
    }

    [Pure]
    public static IEnumerable<TypedFilePathString> CreateFilePaths(this TypedFilePathString path, string fileExtension)
    {
        return Directory
            .GetFiles(path.Value, fileExtension, SearchOption.AllDirectories)
            .Map(d => TypedFilePathString.New(d));
    }

    [Pure]
    public static IEnumerable<TypedFilePathString> UseExcludeFilter(this IEnumerable<TypedFilePathString> source,
        string excludeFilter)
    {
        return excludeFilter.Length == 0 ? source : source.Filter(d => !d.Value.Contains(excludeFilter));
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
    public static string ReplaceSpecialStrings(this string input, Lst<(string Pattern, string ToReplaced)> list)
    {
        list.ForEach(tuple => { input = Regex.Replace(input, tuple.Pattern, tuple.ToReplaced); });
        return input;
    }

    [Pure]
    public static Source<TypedFilePathString, NotUsed> CreateSource(this TypedFilePathString scanPath,
        string fileExtension)
    {
        return Source
            .From(Directory.GetFiles(scanPath.Value, fileExtension,
                SearchOption.AllDirectories).Map(f => TypedFilePathString.New(f)));
    }

    public static Task RunIgnoreAsync<T>(this Source<T, NotUsed> source, ActorMaterializer actorMaterializer) =>
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
    public static Source<Option<TDocument>, NotUsed> FilterExistingUnchangedAsync<TDocument>(
        this Source<Option<TDocument>, NotUsed> source, SchedulerEntry schedulerEntry,
        ComparerModel comparerModel) where TDocument : ElasticDocument
    {
        return source.SelectAsyncUnordered(schedulerEntry.Parallelism, comparerModel.FilterExistingUnchanged);
    }
}