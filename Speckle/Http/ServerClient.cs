using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Speckle.Models;

namespace Speckle.Http
{
  public class ServerClient
  {
    public HttpClient HttpClient;

    public ServerClient(string baseUrl, string ApiToken, int timeoutMinutes = 1)
    {
      HttpClient = new HttpClient(new HttpClientHandler()
      {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip,
      }, true);

      HttpClient.BaseAddress = new Uri(baseUrl);

      HttpClient.DefaultRequestHeaders.Accept.Clear();
      HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      HttpClient.DefaultRequestHeaders.Add("Authorization", ApiToken);

      HttpClient.Timeout = TimeSpan.FromMinutes(timeoutMinutes);
    }

    public async void SaveObjects(IEnumerable<string> objects)
    {
      var request = new HttpRequestMessage();

      request.Method = HttpMethod.Post;

      request.Content = new GzipContent(new StringContent("test"));

      request.RequestUri = new Uri("/objects", UriKind.Relative);

      var response = await HttpClient.SendAsync(request);

      var status = ((int)response.StatusCode).ToString();

      switch (status)
      {
        case "200":
          break;
        default:
          throw new Exception("wow error");
      }
    }

    public async Task<IEnumerable<string>> GetObjects(IEnumerable<string> objects)
    {
      var request = new HttpRequestMessage();

      request.Method = HttpMethod.Get;

      request.RequestUri = new Uri("/objects/" + String.Join(',', objects), UriKind.Relative);

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


  }
}
