using System;
using System.Text.Json;
using DocSearchAIO.DocSearch.TOs;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DocSearchAIO_Test;

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
        var commentDetail = new CommentDetail("CommentText") { };

        var resultJson = JsonSerializer.Serialize(commentDetail);

        var compareString = "{\"commentText\":\"CommentText\"}";

        compareString.Should().Match(resultJson);
    }

    [Fact]
    public void Convert_CommentDetail_To_Appropriate_Json_Return_All_Parameter()
    {
        var commentDetail = new CommentDetail("CommentText")
            { Author = "Author", Date = new DateTime(1986, 12, 5, 10, 31, 22), Id = "1", Initials = "Initials" };

        var resultJson = JsonSerializer.Serialize(commentDetail);

        var expectedJson =
            "{\"commentText\":\"CommentText\",\"author\":\"Author\",\"date\":\"1986-12-05T10:31:22\",\"id\":\"1\",\"initials\":\"Initials\"}";

        expectedJson.Should().Match(resultJson);
    }

    [Fact]
    public void Convert_InitRequest_To_Appropriate_Json_Return_All_Parameter()
    {
        var initRequest =
            "{\"filterEml\":true,\"filterWord\":true,\"filterPowerpoint\":true,\"filterPdf\":true,\"filterMsg\":true,\"filterExcel\":true,\"itemsPerPage\":45}";
        var resultObject = JsonSerializer.Deserialize<InitRequest>(initRequest);

        resultObject.Should().NotBeNull();

        (resultObject is
                { FilterExcel: true, FilterEml: true, FilterMsg: true, FilterPowerpoint: true, FilterPdf: true, FilterWord: true, ItemsPerPage: 45 })
            .Should()
            .BeTrue();
    }

    [Fact]
    public void Convert_InitResponseObject_To_Appropriate_Json_Return_All_Parameter()
    {
        var initResponse = new InitResponseObject
        {
            FilterEml = true, FilterMsg = true, FilterPdf = true, FilterPowerpoint = true, FilterWord = true, EmlFilterActive = true,
            ExcelFilterActive = true, ItemsPerPage = 2, MsgFilterActive = true, PdfFilterActive = true, PowerpointFilterActive = true,
            WordFilterActive = true
        };

        var resultJson = JsonSerializer.Serialize(initResponse);

        var expectedJson =
            "{\"filterExcel\":false,\"filterWord\":true,\"filterPowerpoint\":true,\"filterPdf\":true,\"filterMsg\":true,\"filterEml\":true,\"itemsPerPage\":2,\"wordFilterActive\":true,\"excelFilterActive\":true,\"powerpointFilterActive\":true,\"pdfFilterActive\":true,\"msgFilterActive\":true,\"emlFilterActive\":true}";

        expectedJson.Should().Match(resultJson);
    }
}