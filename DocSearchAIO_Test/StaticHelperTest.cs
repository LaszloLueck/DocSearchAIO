
using System.Collections.Generic;
using System.Threading.Tasks;
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
}