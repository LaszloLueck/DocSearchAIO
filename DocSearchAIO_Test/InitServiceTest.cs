using System.Collections.Generic;
using System.Threading.Tasks;
using DocSearchAIO.Endpoints.Init;
using DocSearchAIO.Services;
using Moq;
using Nest;
using Xunit;

namespace DocSearchAIO_Test;

public class InitServiceTest
{
    // [Fact]
    // public async Task TestInitWithIndices()
    // {
    //     var mockElasticSearchService = new Mock<IElasticSearchService>();
    //     var fakeGetMappingResponse = new 
    //     
    //     
    //     mockElasticSearchService.Setup(ess => ess.IndicesWithPatternAsync(It.IsAny<string>(), It.IsAny<bool>()))
    //         .ReturnsAsync(new GetIndexResponse
    //         {
    //             Indices = new Dictionary<IndexName, IndexState>
    //             {
    //                 {new IndexName("index1"), new IndexState()},
    //                 {new IndexName("index2"), new IndexState()}
    //             }.AsReadOnly()
    //         });
    //
    //     var cfg = new Configuration();
    //     var initRequest = new InitRequest();
    //
    //     var init = new Init(mockElasticSearchService.Object, cfg);
    //     var response = await init.Init(initRequest);
    //
    //     Assert.Equal(cfg, response.Configuration);
    //     Assert.Equal(new[] {"index1", "index2"}, response.IndexNames);
    //     Assert.Equal(initRequest, response.InitRequest);
    // }
    //
    // [Fact]
    // public async Task TestInitWithoutIndices()
    // {
    //     var mockElasticSearchService = new Mock<IElasticSearchService>();
    //     mockElasticSearchService.Setup(ess => ess.IndicesWithPatternAsync(It.IsAny<string>(), It.IsAny<bool>()))
    //         .ReturnsAsync(new GetIndexResponse
    //         {
    //             Indices = null
    //         });
    //
    //     var cfg = new Configuration();
    //     var initRequest = new InitRequest();
    //
    //     var init = new Init(mockElasticSearchService.Object, cfg);
    //     var response = await init.Init(initRequest);
    //
    //     Assert.Equal(cfg, response.Configuration);
    //     Assert.Empty(response.IndexNames);
    //     Assert.Equal(initRequest, response.InitRequest);
    // }
}