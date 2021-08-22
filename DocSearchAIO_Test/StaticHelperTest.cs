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
            var testStringContent = "Fritz jagt im total verwahrlosten Taxi quer durch Berlin!";
            var testStringComment = "Fritz jagt im total verwahrlosten Taxi quer durch München!";
            

            var typedCommentString = new TypedCommentString(testStringComment);
            var typedContentString = new TypedContentString(testStringContent);

            var result = typedCommentString.GenerateTextToSuggest(typedContentString);

            var compareString = "Fritz jagt im total verwahrlosten Taxi quer durch München  Fritz jagt im total verwahrlosten Taxi quer durch Berlin ";

            compareString.Should().Match(result.ToString());
        }
        
    }
}