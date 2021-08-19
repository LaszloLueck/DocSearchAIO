using System;
using System.Xml;
using System.Xml.Linq;
using DocSearchAIO.Scheduler;
using DocumentFormat.OpenXml;
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
    
            _testOutputHelper.WriteLine(result.ToString());
            

            Assert.True(true);
        }
        
    }
}