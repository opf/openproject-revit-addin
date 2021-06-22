using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;

namespace OpenProject.Tests.Mocks
{
  internal class MockHttpClient
  {
    internal HttpClient GetMockClient(Dictionary<string, string> successResponses)
    {
      var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
      mockHttpMessageHandler.Protected()
          .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
          .Returns((HttpRequestMessage request, CancellationToken cancellationToken) => GetMockResponse(request, successResponses));
      return new HttpClient(mockHttpMessageHandler.Object);
    }

    private Task<HttpResponseMessage> GetMockResponse(HttpRequestMessage request, IDictionary<string, string> successResponses)
    {
      var code = HttpStatusCode.InternalServerError;
      var content = "";

      if (successResponses.TryGetValue(request.RequestUri.AbsoluteUri.ToLowerInvariant(), out content))
      {
        code = HttpStatusCode.OK;
      }

      var response = new HttpResponseMessage(code)
      {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
      };
      return Task.FromResult(response);
    }
  }
}
