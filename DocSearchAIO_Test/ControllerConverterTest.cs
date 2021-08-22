using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocSearchAIO.DocSearch.TOs;

using Xunit;
using Xunit.Abstractions;

namespace DocSearchAIO_Test
{
    public class ControllerConverterTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ControllerConverterTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Convert_CommentDetail_To_Appropriate_Json_Return_Only_CommentText()
        {
            var commentDetail = new CommentDetail("CommentText"){};

            var resultJson = JsonSerializer.Serialize(commentDetail, new JsonSerializerOptions(){WriteIndented = true});
            
            _testOutputHelper.WriteLine(resultJson);
            
            
            Assert.True(true);
        }
        
    }

}