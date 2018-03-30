using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMapper;
using Crossroads.Service.HubSpot.Sync.Core.Utilities.Impl;
using Crossroads.Service.HubSpot.Sync.Core.Serialization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Crossroads.Service.HubSpot.Sync.Core.Test.Utilities
{
    public class ContentUtilityTest
    {
        private readonly Mock<FakeHttpMessageHandler> _fakeHttpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<IJsonSerializer> _serializerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<GetSimpleContentOverHttp>> _loggerMock;
        private readonly GetSimpleContentOverHttp _getSimpleContentOverHttp;

        private static readonly List<NormalizedDto> ExpectedArticleList = new List<NormalizedDto>
        {
            new NormalizedDto {
                Author = "Hub Spot",
                Id = "348109414",
                Body = "",
                Image = @"
<p>This is an example blog post.  You can delete this blog post by going to the blog dashboard.</p>
<p>We hope you enjoy your HubSpot!</p>
",
                Title = "Demonstration Blog HttpPost",
                Tags = new List<string> {"one", "two"}
            },
            new NormalizedDto() {
                Author = "Hub Spot",
                Id = "348109414",
                Body = "",
                Image = @"
<p>This is an example blog post.  You can delete this blog post by going to the blog dashboard.</p>
<p>We hope you enjoy your HubSpot!</p>
",
                Title = "Demonstration Blog HttpPost",
                Tags = new List<string> {"one", "two"}
            }
        };

        public ContentUtilityTest()
        {
            _fakeHttpMessageHandlerMock = new Mock<FakeHttpMessageHandler> { CallBase = true };
            _httpClient = new HttpClient(_fakeHttpMessageHandlerMock.Object) { BaseAddress = new Uri("http://localhost:8080/") };
            _mapperMock = new Mock<IMapper>();
            _serializerMock = new Mock<IJsonSerializer>();
            _loggerMock = new Mock<ILogger<GetSimpleContentOverHttp>>();

            _serializerMock.Setup(m => m.Deserialize<ApiObjectRoot>(It.IsAny<string>(), null))
                .Returns(new ApiObjectRoot { Objects = new List<ApiObject> { new ApiObject() }});
            _mapperMock.Setup(m => m.Map<NormalizedDto>(It.IsAny<ApiObject>())).Returns(ExpectedArticleList[0]);
            _fakeHttpMessageHandlerMock.Setup(f => f.Send(It.IsAny<HttpRequestMessage>())).Returns(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("string with stuff in it")
            });

            _getSimpleContentOverHttp = new GetSimpleContentOverHttp(_httpClient, _serializerMock.Object, _mapperMock.Object, _loggerMock.Object);
        }

        [Fact]
        public void NullConstructorArgumentShouldThrowArgumentNullException()
        {
            Action action = () => new GetSimpleContentOverHttp(null, _serializerMock.Object, _mapperMock.Object, _loggerMock.Object);
            action.Should().Throw<ArgumentNullException>();

            action = () => new GetSimpleContentOverHttp(_httpClient, null, _mapperMock.Object, _loggerMock.Object);
            action.Should().Throw<ArgumentNullException>();

            action = () => new GetSimpleContentOverHttp(_httpClient, _serializerMock.Object, null, _loggerMock.Object);
            action.Should().Throw<ArgumentNullException>();

            action = () => new GetSimpleContentOverHttp(_httpClient, _serializerMock.Object, _mapperMock.Object, null);
            action.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\r\n\t")]
        public async Task GetContentListAsyncAndGetContentAsync_ShouldThrowArgumentNullExceptionWhenRequestUriIsNullEmptyOrWhitespace(string requestUri) // GetContentAsync and GetContentListAsync
        {
            async Task<NormalizedDto> func() => await _getSimpleContentOverHttp.GetContentAsync<NormalizedDto, ApiObject>(requestUri);
            async Task<IEnumerable<NormalizedDto>> func2() => await _getSimpleContentOverHttp.GetContentListAsync<NormalizedDto, ApiObject, ApiObjectRoot>(requestUri, list => list?.Objects);

            await Assert.ThrowsAsync<ArgumentNullException>(func);
            await Assert.ThrowsAsync<ArgumentNullException>(func2);
        }

        [Fact]
        public async Task GetContentListAsync_ShouldReturnEmptyCollectionWhenRequestErrorsOut()  // GetContentListAsync
        {
            _fakeHttpMessageHandlerMock.Setup(f => f.Send(It.IsAny<HttpRequestMessage>())).Throws<HttpRequestException>();
            var actual = await _getSimpleContentOverHttp.GetContentListAsync<NormalizedDto, ApiObject, ApiObjectRoot>("fake uri", list => list?.Objects);

            actual.Should().BeEmpty();
        }

        [Fact]
        public async Task GetContentListAsync_ShouldReturnEmptyCollectionWhenDeserializationFails()
        {
            _serializerMock.Setup(s => s.Deserialize<ApiObjectRoot>(It.IsAny<string>(), null)).Throws<Exception>();
            var actual = await _getSimpleContentOverHttp.GetContentListAsync<NormalizedDto, ApiObject, ApiObjectRoot>("fake uri", list => list?.Objects);
            actual.Should().BeEmpty();
        }

        [Fact]
        public async Task GetContentListAsync_ShouldReturnEmptyCollectionWhenNormalizationFails()
        {
            _mapperMock.Setup(m => m.Map<NormalizedDto>(It.IsAny<ApiObject>())).Throws<Exception>();
            var actual = await _getSimpleContentOverHttp.GetContentListAsync<NormalizedDto, ApiObject, ApiObjectRoot>("fake uri", list => list?.Objects);
            actual.Should().BeEmpty();
        }

        [Fact]
        public async Task FetchContentStringAsync_ShouldReturnNullWhenGetRequestFails()
        {
            _fakeHttpMessageHandlerMock.Setup(f => f.Send(It.IsAny<HttpRequestMessage>())).Throws<HttpRequestException>();
            var actual = await _getSimpleContentOverHttp.FetchContentStringAsync("fake uri");
            actual.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\r\n\t")]
        public async Task FetchContentStringAsync_ShouldThrowArgumentNullExceptionWhenArgumentIsNullEmptyOrWhitespace(string requestUriPathAndQuery)
        {
            async Task<string> func() => await _getSimpleContentOverHttp.FetchContentStringAsync(requestUriPathAndQuery);
            await Assert.ThrowsAsync<ArgumentNullException>(func);
        }

        [Fact]
        public void Deserialize_ShouldReturnNullWhenDeserializationFails()
        {
            _serializerMock.Setup(s => s.Deserialize<ApiObjectRoot>(It.IsAny<string>(), null)).Throws<Exception>();
            var actual = _getSimpleContentOverHttp.Deserialize<NormalizedDto>(It.IsAny<string>(), null);
            actual.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\r\n\t")]
        public void Deserialize_ShouldThrowArgumentNullExceptionWhenArgumentIsNullEmptyOrWhitespace(string requestUriPathAndQuery)
        {
            _getSimpleContentOverHttp.Deserialize<ApiObject>(requestUriPathAndQuery).Should().BeNull();
        }

        //[Fact]
        //public async Task ShouldReturnEmptyCollectionWhenObjectMappingFails() // technically, this should 
        //{
        //    _mapperMock.Setup(m => m.Map<NormalizedDto>(It.IsAny<ApiObjectRoot>())).Throws<Exception>();

        //    _getSimpleContentOverHttp = new GetSimpleContentOverHttp(_httpClient, _serializerMock.Object, _mapperMock.Object, _loggerMock.Object);
        //    var actual = await _getSimpleContentOverHttp.GetContentListAsync<NormalizedDto, ApiObject, ApiObjectRoot>("some uri", root => root.Objects);
        //    actual.Should().BeEmpty();
        //}

        [Fact]
        public void Normalize_ShouldReturnNullWhenObjectMappingFails() // technically, this should 
        {
            _mapperMock.Setup(m => m.Map<NormalizedDto>(It.IsAny<ApiObjectRoot>())).Throws<Exception>();
            var actual = _getSimpleContentOverHttp.Normalize<NormalizedDto, ApiObject>(It.IsAny<ApiObject>());
            actual.Should().BeNull();
        }
    }
}