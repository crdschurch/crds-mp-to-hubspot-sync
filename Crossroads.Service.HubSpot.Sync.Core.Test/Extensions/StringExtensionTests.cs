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
            valueToCheck.IsNotNullOrEmpty().Should().BeFalse();
        }
    }
}