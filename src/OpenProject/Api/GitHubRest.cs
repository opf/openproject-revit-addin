using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using OpenProject.Shared;
using RestSharp;
using DataFormat = RestSharp.DataFormat;

namespace OpenProject.Api
{
  public static class GitHubRest
  {
    internal static RestClient Client
    {
      get { return new RestClient(@"https://api.github.com/"); } 
    }

    internal static async Task<List<GitHubRelease>> GetReleases(CancellationTokenSource cancel)
    {
      if (cancel.IsCancellationRequested)
      {
        return null;
      }

      var request = new RestRequest($"repos/{RepositoryInfo.GitHubOwner}/{RepositoryInfo.GitHubRepository}/releases", Method.GET);
      request.AddHeader("Content-Type", "application/json");
      request.RequestFormat = DataFormat.Json;
      request.OnBeforeDeserialization = resp => { resp.ContentType = "application/json"; };

      var response = await DoTaskAsync<List<GitHubRelease>>(request, cancel);

      return cancel.IsCancellationRequested || !CheckResponse(response, HttpStatusCode.OK) ? null : response.Data;
    } 

    internal static GitHubRelease GetLatestRelease()
    {

      var request = new RestRequest($"repos/{RepositoryInfo.GitHubOwner}/{RepositoryInfo.GitHubRepository}/releases/latest", Method.GET);
      request.AddHeader("Content-Type", "application/json");
      request.RequestFormat = DataFormat.Json;

      var response = Client.Execute<GitHubRelease> (request);
      //if cancellation pending or invalid reponse return null, otherwise the data
      return !CheckResponse(response, HttpStatusCode.OK) ? null : response.Data;
    }

    private static async Task<IRestResponse<T>> DoTaskAsync<T>(RestRequest request, CancellationTokenSource cancel) where T : class
    {
      IRestResponse<T> response = null;
      try
      {
        if (cancel != null)
          response = await Client.ExecuteAsync<T>(request, cancel.Token);
      }
      catch (OperationCanceledException ex)
      {
        var gg = ex.Data;
        return null;
      }
      return response;
    }

    private static bool CheckResponse(IRestResponse response, HttpStatusCode expectedCode)
    {
      try
      {
        if (null == response || response.StatusCode != expectedCode)
          return false;
      }
      catch (Exception ex1)
      {
        MessageBox.Show("exception: " + ex1);
      }
      return true;
    }
  }
}
