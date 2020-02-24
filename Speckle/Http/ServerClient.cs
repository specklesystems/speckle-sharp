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

    public ServerClient(string baseUrl, string ApiToken)
    {
      HttpClient = new HttpClient(new HttpClientHandler()
      {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip,
      }, true);

      HttpClient.BaseAddress = new Uri(baseUrl);

      HttpClient.DefaultRequestHeaders.Accept.Clear();
      HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      HttpClient.DefaultRequestHeaders.Add("Authorization", ApiToken);

      HttpClient.Timeout = TimeSpan.FromMinutes(1);
    }

    public async void SaveObjects(IEnumerable<Base> objects)
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

    public async Task<object> GetObjects(IEnumerable<string> objects)
    {
      return null;
    }


  }
}
