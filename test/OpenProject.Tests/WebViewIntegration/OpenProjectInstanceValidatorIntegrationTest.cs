using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using OpenProject.WebViewIntegration;
using Xunit;

namespace OpenProject.Tests.WebViewIntegration
{
  /// <summary>
  /// This test suite runs the <see cref="OpenProjectInstanceValidator"/> against a real external running instance of
  /// the OpenProject API.
  /// </summary>
  public class OpenProjectInstanceValidatorIntegrationTest
  {
    [Fact]
    public async Task ReturnsTrueForExternalRunningInstance()
    {
      // Arrange
      var factoryMock = new Mock<IHttpClientFactory>();
      factoryMock.Setup(x => x.CreateClient(It.IsAny<string>()))
        .Returns((string name) => new HttpClient());

      var validator = new OpenProjectInstanceValidator(factoryMock.Object);

      // Act
      var (isValid, instanceBaseUrl) = await validator
        .IsValidOpenProjectInstanceAsync("https://wieland.openproject.com");

      // Assert
      Assert.True(isValid);
      Assert.Equal("https://wieland.openproject.com", instanceBaseUrl);
    }
  }
}