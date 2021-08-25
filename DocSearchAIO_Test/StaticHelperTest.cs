using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using DocSearchAIO.Scheduler;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DocSearchAIO_Test
{
    public class StaticHelperTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public StaticHelperTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Build_a_content_and_comment_string_for_suggest()
        {
            var testStringContent = "Fritz jagt im total verwahrlosten Taxi quer durch Berlin";
            var testStringComment = "Fritz jagt im total verwahrlosten Taxi quer durch München";


            var typedCommentString = new TypedCommentString(testStringComment);
            var typedContentString = new TypedContentString(testStringContent);

            var result = typedCommentString.GenerateTextToSuggest(typedContentString);

            var compareString = "Fritz jagt im total verwahrlosten Taxi quer durch München Fritz jagt im total verwahrlosten Taxi quer durch Berlin";

            compareString.Should().Match(result.ToString());
        }

        [Fact]
        public void Build_a_content_and_comment_string_for_suggest_with_replaces()
        {
            var testStringContent = "T§h$e &q+u/i?ck \\b[r=o<wn fo>x jump´s ove'r the l@a{z€y do!§g}";
            var testStringComment = "T§h$e &q+u/i?ck \\b[r=o<wn fo>x jump´s ove'r the l@a{z€y do!§g}";

            var typedCommentString = new TypedCommentString(testStringComment);
            var typedContentString = new TypedContentString(testStringContent);

            var result = typedCommentString.GenerateTextToSuggest(typedContentString);
            var expectedString = "The quick brown fox jumps over the lazy dog The quick brown fox jumps over the lazy dog";


            expectedString.Should().Match(result.Value);
        }

        [Fact]
        public void Build_a_suggest_array_from_a_text_string()
        {
            var testString = new TypedSuggestString("The quick brown fo jumps over the lazy do");
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
        public void Build_ContentString_From_TextElement()
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
            var pList = new List<OpenXmlElement>{p1, p2};

            var result = pList.ContentString();

            "Text List A Text List B".Should().Match(result);

        }
        
    }
}