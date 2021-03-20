using OpenProject.WebViewIntegration;
using System.Threading.Tasks;
using Xunit;

namespace OpenProject.Tests.WebViewIntegration
{
  public static class OpenProjectInstanceValidatorTests
  {
    public class IsValidOpenProjectInstanceAsync
    {
      [Theory]
      [InlineData("https://community.openproject.org/api/v3", "https://community.openproject.org")]
      [InlineData("https://COMMUNITY.OPENPROJECT.ORG/api/v3", "https://community.openproject.org")]
      [InlineData("http://community.openproject.org/api/v3", "http://community.openproject.org")]
      [InlineData("https://wieland.openproject.com/api/v3", "https://wieland.openproject.com")]
      [InlineData("https://community.openproject.org/api/v3/", "https://community.openproject.org")]
      [InlineData("https://wieland.openproject.com/api/v3/", "https://wieland.openproject.com")]
      [InlineData("https://community.openproject.org:443/api/v3", "https://community.openproject.org:443")]
      [InlineData("https://wieland.openproject.com:443/api/v3", "https://wieland.openproject.com:443")]
      public async Task ReturnsTrueForActualInstances(string instanceUrl, string expectedBaseUrl)
      {
        var actual = await OpenProjectInstanceValidator.IsValidOpenProjectInstanceAsync(instanceUrl);
        Assert.True(actual.isValid);
        Assert.Equal(expectedBaseUrl, actual.instanceBaseUrl);
      }

      [Theory]
      [InlineData("wieland", "https://wieland.openproject.com")]
      [InlineData("community", "https://community.openproject.com")]
      public async Task ReturnsTrueForJustTheInstanceName(string instanceName, string expectedBaseUrl)
      {
        var actual = await OpenProjectInstanceValidator.IsValidOpenProjectInstanceAsync(instanceName);
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
        var actual = await OpenProjectInstanceValidator.IsValidOpenProjectInstanceAsync(instanceUrl);
        Assert.True(actual.isValid);
        Assert.Equal(expectedBaseUrl, actual.instanceBaseUrl);
      }

      [Theory]
      [InlineData("community.openproject.org/", "https://community.openproject.org")]
      [InlineData("wieland.openproject.com/", "https://wieland.openproject.com")]
      [InlineData("community.openproject.org", "https://community.openproject.org")]
      [InlineData("wieland.openproject.com", "https://wieland.openproject.com")]
      [InlineData("community.openproject.org:443", "https://community.openproject.org:443")]
      public async Task ReturnsTrueForJustTheUrlWithoutProtocol_WithoutApiPath(string instanceUrl, string expectedBaseUrl)
      {
        var actual = await OpenProjectInstanceValidator.IsValidOpenProjectInstanceAsync(instanceUrl);
        Assert.True(actual.isValid);
        Assert.Equal(expectedBaseUrl, actual.instanceBaseUrl);
      }

      [Theory]
      [InlineData("www.google.com")]
      [InlineData("www.example.com")]
      public async Task ReturnsFalseForUrlWithoutProtocolWhenNotAValidInstance(string instanceUrl)
      {
        var actual = await OpenProjectInstanceValidator.IsValidOpenProjectInstanceAsync(instanceUrl);
        Assert.False(actual.isValid);
        Assert.Null(actual.instanceBaseUrl);
      }

      [Theory]
      [InlineData("ea933668-138e-44e5-a93e-f0d672148f04")]
      [InlineData("wielandä")]
      [InlineData("😀")]
      public async Task ReturnsFalseForInvalidInstanceNames(string instanceName)
      {
        var actual = await OpenProjectInstanceValidator.IsValidOpenProjectInstanceAsync(instanceName);
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
        var actual = await OpenProjectInstanceValidator.IsValidOpenProjectInstanceAsync(instanceUrl);
        Assert.False(actual.isValid);
        Assert.Null(actual.instanceBaseUrl);
      }
    }
  }
}
