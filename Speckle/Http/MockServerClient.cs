using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Speckle.Core;
using Speckle.Models;

namespace Speckle.Http
{
  public class MockServerClient
  {
    public HttpClient HttpClient;

    public MockServerClient(string baseUrl, string apiToken, int timeoutMinutes = 3)
    {
      HttpClient = new HttpClient(new HttpClientHandler()
      {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip,
      }, true);

      HttpClient.BaseAddress = new Uri(baseUrl);

      HttpClient.DefaultRequestHeaders.Accept.Clear();
      HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      HttpClient.DefaultRequestHeaders.Add("Authorization", apiToken);

      HttpClient.Timeout = TimeSpan.FromMinutes(timeoutMinutes);
    }

    #region Object API

    public async void SaveObjects(IEnumerable<string> objects, string streamId)
    {
      var request = new HttpRequestMessage()
      {
        RequestUri = new Uri($"/streams/{streamId}/objects", UriKind.Relative),
        Method = HttpMethod.Post
      };

      request.Content = new GzipContent(new StringContent("test"));

      var response = await HttpClient.SendAsync(request);

      var status = ((int)response.StatusCode).ToString();

      switch (status)
      {
        case "200":
          // TODO: return success
          break;
        case "401":
          throw new Exception("Not authorized.");
        default:
          throw new Exception("Fail whale.");
      }
    }

    public async Task<IEnumerable<string>> GetObjects(IEnumerable<string> objects, string streamId)
    {
      var request = new HttpRequestMessage()
      {
        Method = HttpMethod.Get,
        RequestUri = new Uri($"/streams/{streamId}/objects" + String.Join(',', objects), UriKind.Relative)
      };

      var response = await HttpClient.SendAsync(request);

      var status = ((int)response.StatusCode).ToString();

      switch (status)
      {
        case "200":
          break;
        default:
          throw new Exception("wow error");
      }

      return null;
    }

    #endregion

    #region Streams API
    public async Task<Stream> GetStream(string streamId)
    {
      var request = new HttpRequestMessage()
      {
        RequestUri = new Uri($"/streams/{streamId}", UriKind.Relative),
        Method = HttpMethod.Get
      };

      throw new NotImplementedException();
    }

    public async void UpdateStream()
    {
      throw new NotImplementedException();
    }
    #endregion

    #region Pre-flight API

    public async void PreflightPurge(IEnumerable<string> objectIds, string streamId)
    {
      // TODO: ask the server what objects it already has.

      var request = new HttpRequestMessage()
      {
        RequestUri = new Uri($"/streams/{streamId}/preflight", UriKind.Relative),
        Method = HttpMethod.Post
      };

      request.Content = new GzipContent(new StringContent("test"));


      throw new NotImplementedException();
    }

    #endregion
  }
}
