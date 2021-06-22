using Moq;
using OpenProject.Tests.Mocks;
using OpenProject.WebViewIntegration;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace OpenProject.Tests.WebViewIntegration
{
  public static class OpenProjectInstanceValidatorTests
  {
    public class IsValidOpenProjectInstanceAsync
    {

      private readonly OpenProjectInstanceValidator _validator;

      public IsValidOpenProjectInstanceAsync()
      {
        var successResponse = new Newtonsoft.Json.Linq.JObject
        {
          ["_type"] = "Root",
          ["instanceName"] = "op-instance"
        };

        var mockedHttpResponses = new Dictionary<string, string>()
        {
          {"https://community.openproject.org/api/v3", successResponse.ToString()},
          {"https://community.openproject.com/api/v3", successResponse.ToString()},
          {"http://community.openproject.org/api/v3", successResponse.ToString()},
          {"https://wieland.openproject.com/api/v3", successResponse.ToString()},
          {"https://test.openproject.com:8443/api/v3", successResponse.ToString()}
        };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
          .Returns((string name) => { return new MockHttpClient().GetMockClient(mockedHttpResponses); });

        _validator = new OpenProjectInstanceValidator(factoryMock.Object);
      }

      [Theory]
      [InlineData("https://community.openproject.org/api/v3", "https://community.openproject.org")]
      [InlineData("https://COMMUNITY.OPENPROJECT.ORG/api/v3", "https://community.openproject.org")]
      [InlineData("http://community.openproject.org/api/v3", "http://community.openproject.org")]
      [InlineData("https://wieland.openproject.com/api/v3", "https://wieland.openproject.com")]
      [InlineData("https://community.openproject.org/api/v3/", "https://community.openproject.org")]
      [InlineData("https://wieland.openproject.com/api/v3/", "https://wieland.openproject.com")]
      [InlineData("https://community.openproject.org:443/api/v3", "https://community.openproject.org")]
      [InlineData("https://wieland.openproject.com:443/api/v3", "https://wieland.openproject.com")]
      [InlineData("https://test.openproject.com:8443/api/v3", "https://test.openproject.com:8443")]
      public async Task ReturnsTrueForActualInstances(string instanceUrl, string expectedBaseUrl)
      {
        var actual = await _validator.IsValidOpenProjectInstanceAsync(instanceUrl);
        Assert.True(actual.isValid);
        Assert.Equal(expectedBaseUrl, actual.instanceBaseUrl);
      }

      [Theory]
      [InlineData("wieland", "https://wieland.openproject.com")]
      [InlineData("community", "https://community.openproject.com")]
      public async Task ReturnsTrueForJustTheInstanceName(string instanceName, string expectedBaseUrl)
      {
        var actual = await _validator.IsValidOpenProjectInstanceAsync(instanceName);
        Assert.True(actual.isValid);
        Assert.Equal(expectedBaseUrl, actual.instanceBaseUrl);
      }

      [Theory]
      [InlineData("community.openproject.org/api/v3", "https://community.openproject.org")]
      [InlineData("wieland.openproject.com/api/v3", "https://wieland.openproject.com")]
      [InlineData("community.openproject.org/api/v3/", "https://community.openproject.org")]
      [InlineData("wieland.openproject.com/api/v3/", "https://wieland.openproject.com")]
      public async Task ReturnsTrueForJustTheUrlWithoutProtocol(string instanceUrl, string expectedBaseUrl)
      {
        var actual = await _validator.IsValidOpenProjectInstanceAsync(instanceUrl);
        Assert.True(actual.isValid);
        Assert.Equal(expectedBaseUrl, actual.instanceBaseUrl);
      }

      [Theory]
      [InlineData("community.openproject.org/", "https://community.openproject.org")]
      [InlineData("wieland.openproject.com/", "https://wieland.openproject.com")]
      [InlineData("community.openproject.org", "https://community.openproject.org")]
      [InlineData("wieland.openproject.com", "https://wieland.openproject.com")]
      [InlineData("community.openproject.org:443", "https://community.openproject.org")]
      [InlineData("test.openproject.com:8443", "https://test.openproject.com:8443")]
      public async Task ReturnsTrueForJustTheUrlWithoutProtocol_WithoutApiPath(string instanceUrl, string expectedBaseUrl)
      {
        var actual = await _validator.IsValidOpenProjectInstanceAsync(instanceUrl);
        Assert.True(actual.isValid);
        Assert.Equal(expectedBaseUrl, actual.instanceBaseUrl);
      }

      [Theory]
      [InlineData("https://community.openproject.org/", "https://community.openproject.org")]
      [InlineData("https://wieland.openproject.com/", "https://wieland.openproject.com")]
      [InlineData("https://community.openproject.org", "https://community.openproject.org")]
      [InlineData("https://wieland.openproject.com", "https://wieland.openproject.com")]
      [InlineData("https://community.openproject.org:443", "https://community.openproject.org")]
      [InlineData("https://test.openproject.com:8443", "https://test.openproject.com:8443")]
      public async Task ReturnsTrueForJustTheUrlWithProtocol_WithoutApiPath(string instanceUrl, string expectedBaseUrl)
      {
        var actual = await _validator.IsValidOpenProjectInstanceAsync(instanceUrl);
        Assert.True(actual.isValid);
        Assert.Equal(expectedBaseUrl, actual.instanceBaseUrl);
      }

      [Theory]
      [InlineData("file:///some/strange/human/providing/strange/urls.txt")]
      [InlineData("wss://javascript.info")]
      public async Task ReturnsFalseForUrlWithoutHttoScheme(string instanceUrl)
      {
        var actual = await _validator.IsValidOpenProjectInstanceAsync(instanceUrl);
        Assert.False(actual.isValid);
        Assert.Null(actual.instanceBaseUrl);
      }

      [Theory]
      [InlineData("www.google.com")]
      [InlineData("www.example.com")]
      public async Task ReturnsFalseForUrlWithoutProtocolWhenNotAValidInstance(string instanceUrl)
      {
        var actual = await _validator.IsValidOpenProjectInstanceAsync(instanceUrl);
        Assert.False(actual.isValid);
        Assert.Null(actual.instanceBaseUrl);
      }

      [Theory]
      [InlineData("ea933668-138e-44e5-a93e-f0d672148f04")]
      [InlineData("wielandä")]
      [InlineData("😀")]
      public async Task ReturnsFalseForInvalidInstanceNames(string instanceName)
      {
        var actual = await _validator.IsValidOpenProjectInstanceAsync(instanceName);
        Assert.False(actual.isValid);
        Assert.Null(actual.instanceBaseUrl);
      }

      [Theory]
      [InlineData("http://www.google.com")]
      [InlineData("http://www.example.com")]
      [InlineData("http://www.dangl-it.com")]
      [InlineData("http://ea933668-138e-44e5-a93e-f0d672148f04")]
      public async Task ReturnsFalseForNoInstances(string instanceUrl)
      {
        var actual = await _validator.IsValidOpenProjectInstanceAsync(instanceUrl);
        Assert.False(actual.isValid);
        Assert.Null(actual.instanceBaseUrl);
      }
    }
  }
}
