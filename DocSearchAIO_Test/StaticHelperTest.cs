using System;
using System.Xml;
using System.Xml.Linq;
using DocSearchAIO.Scheduler;
using DocumentFormat.OpenXml;
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
    }
}