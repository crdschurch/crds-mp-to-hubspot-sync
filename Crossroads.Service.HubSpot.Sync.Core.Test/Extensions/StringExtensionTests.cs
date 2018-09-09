using System;
using FluentAssertions;
using Xunit;

namespace Crossroads.Service.HubSpot.Sync.Core.Test.Extensions
{
    public class StringExtensionTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("     ")]
        [InlineData("\r\n\t")]
        public void NullEmptyOrWhitespaceShouldReturnTrue(string valueToCheck)
        {
            valueToCheck.IsNullOrEmpty().Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("     ")]
        [InlineData("\r\n\t")]
        public void not_null_empty_or_whitespace_should_return_false(string valueToCheck)
        {
            valueToCheck.IsNotNullOrEmpty().Should().BeFalse();
        }
    }
}