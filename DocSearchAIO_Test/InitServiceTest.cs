using System.Threading.Tasks;
using DocSearchAIO.Classes;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.ServiceHooks;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.Endpoints.Init;
using DocSearchAIO.Services;
using Moq;
using Xunit;

namespace DocSearchAIO_Test;

public class InitServiceTest
{
    private readonly InitRequest _initRequest = new InitRequest
    {
        FilterEml = true,
        FilterMsg = false,
        FilterExcel = false,
        FilterWord = true,
        FilterPowerpoint = false,
        FilterPdf = true
    };
    private readonly Mock<IConfigurationUpdater> _configMock = new Mock<IConfigurationUpdater>();
    private readonly Mock<IElasticSearchService> _elasticSearchServiceMock = new Mock<IElasticSearchService>();

    [Fact]
    public async Task
        Given_IndicesWithPatternAsync_Returns_IndexNames_When_Init_Is_Called_Then_Result_Should_Contain_IndexNames()
    {
        // Arrange
        var indexNames = new[] {"officedocuments-eml", "officedocuments-word", "officedocuments-excel"};
        var indicesResponse = new IndexResponseObject(indexNames);
        _elasticSearchServiceMock.Setup(x => x.IndicesWithPatternAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(indicesResponse);

        var cfg = new ConfigurationObject
        {
            IndexName = "officedocuments"
        };
        var eml = new SchedulerEntry
        {
            Active = true,
            IndexSuffix = "eml"
        };
        cfg.Processing.Add(nameof(EmlElasticDocument), eml);
        _configMock.Setup(x => x.ReadConfiguration()).Returns(cfg);

        var sut = new InitService(_elasticSearchServiceMock.Object, _configMock.Object);

        // Act
        var result = await sut.Init(_initRequest);

        // Assert
        Assert.True(result.EmlFilterActive);
        Assert.True(result.FilterEml);
        Assert.False(result.FilterWord);
        Assert.False(result.WordFilterActive);
        Assert.False(result.FilterExcel);
        Assert.False(result.ExcelFilterActive);
    }
}