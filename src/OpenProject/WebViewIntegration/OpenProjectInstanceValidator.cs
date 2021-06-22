using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenProject.WebViewIntegration
{
  public class OpenProjectInstanceValidator
  {
    private readonly IHttpClientFactory _httpClientFactory;

    public OpenProjectInstanceValidator(IHttpClientFactory httpClientFactory)
    {
      _httpClientFactory = httpClientFactory;
    }

    public async Task<(bool isValid, string instanceBaseUrl)> IsValidOpenProjectInstanceAsync(string instanceNameOrUrl)
    {
      var instanceUrl = GetInstanceUrl(instanceNameOrUrl);
      if (string.IsNullOrWhiteSpace(instanceUrl))
      {
        return (false, null);
      }

      var response = await GetHttpResponseAsync(instanceUrl);
      if (response == null)
      {
        // This means there was an Http error, e.g. unable to make a connection
        // or a failure to resolve the domain. So it's either not reachable or
        // not a valid domain, which both warrants a false return from this method
        return (false, null);
      }

      switch (response.StatusCode)
      {
        case HttpStatusCode.OK:
        case HttpStatusCode.Unauthorized:
          var validationResult = await IsLikelyOpenProjectHttpResponseContentAsync(response);
          if (validationResult)
          {
            return (true, Regex.Replace(instanceUrl.ToLower().TrimEnd('/'), "/api/v3$", string.Empty));
          }
          break;
      }

      return (false, null);
    }

    private static string GetInstanceUrl(string instanceNameOrUrl)
    {
      const string apiPathSuffix = "/api/v3";
      var hasApiSuffix = instanceNameOrUrl.TrimEnd('/').EndsWith(apiPathSuffix, StringComparison.InvariantCultureIgnoreCase);

      string appendSuffix(string uri)
      {
        var suffix = hasApiSuffix ? "" : apiPathSuffix;
        return uri + suffix;
      }

      if (Uri.TryCreate(instanceNameOrUrl, UriKind.Absolute, out var instanceUri)
        && Regex.IsMatch(instanceUri.Scheme, "^https?$"))
      {
        return appendSuffix(instanceUri.AbsoluteUri.TrimEnd('/'));
      }

      var subDomainRegexPattern = "^[a-zA-Z0-9-]+$";
      if (Regex.IsMatch(instanceNameOrUrl, subDomainRegexPattern))
      {
        return $"https://{instanceNameOrUrl}.openproject.com{apiPathSuffix}";
      }

      if (Uri.TryCreate($"https://{instanceNameOrUrl}", UriKind.Absolute, out instanceUri))
      {
        return appendSuffix(instanceUri.AbsoluteUri.TrimEnd('/'));
      }

      return null;
    }

    private async Task<HttpResponseMessage> GetHttpResponseAsync(string instanceUrl)
    {
      using var httpClient = _httpClientFactory.CreateClient(nameof(OpenProjectInstanceValidator));
      try
      {
        var response = await httpClient.GetAsync(instanceUrl);
        return response;
      }
      catch
      {
        return null;
      }
    }

    private static async Task<bool> IsLikelyOpenProjectHttpResponseContentAsync(HttpResponseMessage httpResponse)
    {
      var responseContent = await httpResponse.Content.ReadAsStringAsync();
      try
      {
        var jObject = JObject.Parse(responseContent);
        if (httpResponse.IsSuccessStatusCode)
        {
          return IsLikelyOpenProjectInstanceRoot(jObject);
        }

        return IsLikelyOpenProjectErrorInstance(jObject);
      }
      catch
      {
        return false;
      }
    }

    private static bool IsLikelyOpenProjectInstanceRoot(JObject jObject)
    {
      return (jObject["_type"]?.ToString().Equals("Root", StringComparison.OrdinalIgnoreCase) ?? false)
        && !string.IsNullOrWhiteSpace(jObject["instanceName"]?.ToString());
    }

    private static bool IsLikelyOpenProjectErrorInstance(JObject jObject)
    {
      return (jObject["_type"]?.ToString().Equals("Error", StringComparison.OrdinalIgnoreCase) ?? false)
        && !string.IsNullOrWhiteSpace(jObject["errorIdentifier"]?.ToString());
    }
  }
}
