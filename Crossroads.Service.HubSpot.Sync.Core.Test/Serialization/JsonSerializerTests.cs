using System;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using JsonSerializer = Crossroads.Service.HubSpot.Sync.Core.Serialization.Impl.JsonSerializer;

namespace Crossroads.Service.HubSpot.Sync.Core.Test.Serialization
{
    public class JsonSerializerTests
    {
        private readonly Mock<ILogger<JsonSerializer>> _logger = new Mock<ILogger<JsonSerializer>>();
        private readonly JsonSerializer _jsonSerializer;

        public JsonSerializerTests()
        {
            _jsonSerializer = new JsonSerializer(_logger.Object);
        }

        private const string ArticleJson = @"
{
    ""article"": {
        ""id"": 1,
        ""title"": ""Test Article 1 Title"",
        ""body"": ""<p>Body content</p>"",
        ""date"": ""2018-02-08"",
        ""author"": ""Me Me"",
        ""version"": ""2""
    }
}";

        private readonly NormalizedDto _expectation = new NormalizedDto
        {
            Id = "1",
            Title = "Test Article 1 Title",
            Body = "<p>Body content</p>",
            Date = DateTime.Parse("2018-02-08"),
            Author = "Me Me",
            Version = "2"
        };

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("     ")]
        [InlineData("\r\n\t")]
        public void ShouldReturnDefaultOfTWhenDeserializationCandidateIsNullEmptyOrWhiteSpace(string valueToDeserialize)
        {
            _jsonSerializer.Deserialize<Object>(valueToDeserialize).Should().BeEquivalentTo(default(Object));
        }

        /// <summary>
        /// Needs the argument "article" to parse correctly.
        /// </summary>
        [Fact]
        public void ShouldReturnArticleWhenCorrectRootNodeSelectorIsProvided()
        {
            _jsonSerializer.Deserialize<NormalizedDto>(ArticleJson, "article").Should().BeEquivalentTo(_expectation);
        }

        [Theory]
        [InlineData("Article")]
        [InlineData("articles")]
        public void ShouldThrowExceptionWhenIncorrectRootNodeSelectorIsProvided(string jsonRootNodeSelector)
        {
            Action action = ()=> _jsonSerializer.Deserialize<NormalizedDto>(ArticleJson, jsonRootNodeSelector);
            action.Should().Throw<NullReferenceException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("\r\n\t")]
        public void ShouldReturnEmptyObjectWhenNullEmptyOrWhitespaceRootNodeSelectorIsProvided(string jsonRootNodeSelector)
        {
            var result = _jsonSerializer.Deserialize<NormalizedDto>(ArticleJson, jsonRootNodeSelector);
            result.Should().NotBeNull();

            result.Id.Should().BeNull();
            result.Title.Should().BeNull();
            result.Body.Should().BeNull();
            result.Date.Should().Be(DateTime.MinValue);
            result.Author.Should().BeNull();
            result.Version.Should().BeNull();
        }
    }
}