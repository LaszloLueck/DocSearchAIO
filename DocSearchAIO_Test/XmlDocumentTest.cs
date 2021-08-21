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
    public class XmlDocumentTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public XmlDocumentTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Crunch_Table_Structure_And_Return_Text()
        {
            var testString = "Fritz jagt im total verwahrlosten Taxi quer durch München!";

            var typedCommentString = new TypedCommentString(testString);
            var typedContentString = new TypedContentString(testString);

            var result = typedCommentString.GenerateTextToSuggest(typedContentString);

            var compareString = "Fritz jagt im total verwahrlosten Taxi quer durch München  Fritz jagt im total verwahrlosten Taxi quer durch München ";

            compareString.Should().Match(result.ToString());
        }
        
    }
}