using System.Threading.Tasks;
using DocSearchAIO.Configuration;
using DocSearchAIO.DocSearch.Services;
using DocSearchAIO.Endpoints.Init;
using DocSearchAIO.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace DocSearchAIO_Test;

public class InitServiceTest
{
    private readonly Mock<IConfigurationSection> configurationSectionMock = new Mock<IConfigurationSection>();
    private readonly InitRequest _initRequest = new InitRequest();
    private readonly Mock<IConfiguration> _configMock = new Mock<IConfiguration>();
    private readonly Mock<IElasticSearchService> _elasticSearchServiceMock = new Mock<IElasticSearchService>();

    //[Fact]
    // public async Task Given_IndicesWithPatternAsync_Returns_IndexNames_When_Init_Is_Called_Then_Result_Should_Contain_IndexNames()
    // {
    //     // Arrange
    //     var indexNames = new[] {"index1", "index2", "index3"};
    //     var indicesResponse = new IndexResponseObject(indexNames);
    //     _elasticSearchServiceMock.Setup(x => x.IndicesWithPatternAsync(It.IsAny<string>(), It.IsAny<bool>()))
    //         .ReturnsAsync(indicesResponse);
    //
    //     var cfg = new ConfigurationObject();
    //     //configurationSectionMock.Setup(d => d.GetValue<ConfigurationObject>("configurationObject")).Returns(cfg);
    //     _configMock.Setup(d => d.GetSection(It.Is<string>(s => s == "configurationObject"))).Returns(cfg);
    //
    //     var sut = new InitService(_elasticSearchServiceMock.Object, _configMock.Object);
    //
    //     // Act
    //     var result = await sut.Init(_initRequest);
    //
    //     // Assert
    //     Assert.Equal(false, result.FilterEml);
    // }

    // [Fact]
    // public async Task Given_IndicesWithPatternAsync_Returns_Null_When_Init_Is_Called_Then_Result_Should_Contain_Empty_Array()
    // {
    //     // Arrange
    //     var indicesResponse = new IndicesResponse {IndexNames = null};
    //     _elasticSearchServiceMock.Setup(x => x.IndicesWithPatternAsync(It.IsAny<string>()))
    //         .ReturnsAsync(indicesResponse);
    //     var sut = new InitService(_config, _elasticSearchServiceMock.Object);
    //
    //     // Act
    //     var result = await sut.Init(_initRequest);
    //
    //     // Assert
    //     Assert.Empty(result.IndexNames);
    // }
}