
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using DocSearchAIO.Classes;
using DocSearchAIO.Utilities;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using LanguageExt;
using Xunit;

namespace DocSearchAIO_Test;

public class StaticHelperTest
{
    //if we need a Console Out, we mus inherit ITestOutputHelper testOutputHelper

    private readonly ActorSystem _actorSystem = ActorSystem.Create("testActorSystem");

    [Fact]
    public async Task Build_a_content_and_comment_string_for_suggest_Async()
    {
        const string testStringContent = "Fritz jagt im total verwahrlosten Taxi quer durch Berlin";
        const string testStringComment = "Fritz jagt im total verwahrlosten Taxi quer durch München";


        var typedCommentString = TypedCommentString.New(testStringComment);
        var typedContentString = TypedContentString.New(testStringContent);

        var result = await typedCommentString.GenerateTextToSuggestAsync(typedContentString);

        var compareString =
            "Fritz jagt im total verwahrlosten Taxi quer durch München Fritz jagt im total verwahrlosten Taxi quer durch Berlin";

        compareString.Should().Match(result.ToString());
    }

    [Fact]
    public async Task Build_a_content_and_comment_string_for_suggest_with_replaces_Async()
    {
        const string testStringContent = "T§h$e &q+u/i?ck \\b[r=o<wn fo>x jump´s ove'r the l@a{z€y do!§g}";
        const string testStringComment = "T§h$e &q+u/i?ck \\b[r=o<wn fo>x jump´s ove'r the l@a{z€y do!§g}";

        var typedCommentString = TypedCommentString.New(testStringComment);
        var typedContentString = TypedContentString.New(testStringContent);

        var result = typedCommentString.GenerateTextToSuggestAsync(typedContentString);
        var expectedString =
            "The quick brown fox jumps over the lazy dog The quick brown fox jumps over the lazy dog";

        var asExpectedTask = (await result).Value;
        expectedString.Should().Match(asExpectedTask);
    }

    [Fact]
    public void Build_a_suggest_array_from_a_text_string()
    {
        var testString = TypedSuggestString.New("The quick brown fo jumps over the lazy do");
        var result = testString.GenerateSearchAsYouTypeArray();

        Assert.Collection(result,
            item => Assert.Contains("the", item),
            item => Assert.Contains("quick", item),
            item => Assert.Contains("brown", item),
            item => Assert.Contains("jumps", item),
            item => Assert.Contains("over", item),
            item => Assert.Contains("lazy", item)
        );
    }

    [Fact]
    public async Task Build_ContentString_From_TextElement_Async()
    {
        var list1 = new List<OpenXmlElement>
        {
            new Text("Text List A"),
        };
        var list2 = new List<OpenXmlElement>
        {
            new Text("Text List B"),
        };

        var p1 = new Paragraph(list1);
        var p2 = new Paragraph(list2);
        var pList = new List<OpenXmlElement> { p1, p2 };

        var result = await pList.ContentStringAsync();

        "Text List A Text List B".Should().Match(result);
    }

    [Fact]
    public void Replace_Characters_From_String()
    {
        var testString = "The quick brown fox jumps over the lazy dog";

        var listToReplace = Prelude.List(
            ("o", ""),
            ("u", "")
        );

        var result = testString.ReplaceSpecialStrings(listToReplace);
        "The qick brwn fx jmps ver the lazy dg".Should().Match(result);
    }
    
    [Fact]
    public void TestUseExcludeFileFilterWithExcludeFilter()
    {
        var source = Source.From(new[]
        {
            new TypedFilePathString("file1.txt"),
            new TypedFilePathString("file2.tyt"),
            new TypedFilePathString("file3.tzt")
        }); 

        var excludeFilter = ".txt";
        var filteredSource = source.UseExcludeFileFilter(excludeFilter);

        var expectedResult = new[]
        {
            "file2.tyt",
            "file3.tzt"
        };

        var result = filteredSource.RunWith(Sink.Seq<string>(), _actorSystem.Materializer());

        Assert.Equal(expectedResult, result.Result);
    }

    [Fact]
    public void TestUseExcludeFileFilterWithoutExcludeFilter()
    {
        var source = Source.From(new[]
        {
            new TypedFilePathString("file1.txt"),
            new TypedFilePathString("file2.txt"),
            new TypedFilePathString("file3.txt")
        });

        var excludeFilter = string.Empty;
        var filteredSource = source.UseExcludeFileFilter(excludeFilter);

        var expectedResult = new[]
        {
            "file1.txt",
            "file2.txt",
            "file3.txt"
        };

        var result = filteredSource.RunWith(Sink.Seq<string>(), _actorSystem.Materializer());

        Assert.Equal(expectedResult, result.Result);
    }
    
    [Fact]
    public void TestCreateSourceWithFileExtension()
    {
        var scanPath = TypedFilePathString.New(Directory.GetCurrentDirectory());
        var fileExtension = "*.txt";
        var source = scanPath.CreateSource(fileExtension);

        var expectedResult = Directory.GetFiles(scanPath.Value, fileExtension,
            SearchOption.AllDirectories).Map(f => TypedFilePathString.New(f));

        var result = source.RunWith(Sink.Seq<TypedFilePathString>(), _actorSystem.Materializer());

        Assert.Equal(expectedResult, result.Result);
    }

    [Fact]
    public void TestCreateSourceWithoutFileExtension()
    {
        var scanPath = TypedFilePathString.New(Directory.GetCurrentDirectory());
        var fileExtension = "*.*";
        var source = scanPath.CreateSource(fileExtension);

        var expectedResult = Directory.GetFiles(scanPath.Value, fileExtension,
            SearchOption.AllDirectories).Map(f => TypedFilePathString.New(f));

        var result = source.RunWith(Sink.Seq<TypedFilePathString>(), _actorSystem.Materializer());

        Assert.Equal(expectedResult, result.Result);
    }
    
}