using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Crossroads.Service.HubSpot.Sync.ApplicationServices.Test
{
    /// <summary>
    /// https://github.com/dotnet/corefx/issues/1624
    /// </summary>
    internal static class MockHttpClientHandlerExtensions
    {
        public static void SetupGetStringAsync(this Mock<HttpClientHandler> mockHandler, Uri requestUri, string response)
        {
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(message => message.RequestUri == requestUri), ItExpr.IsAny<CancellationToken>())
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response) }));
        }
    }
}
