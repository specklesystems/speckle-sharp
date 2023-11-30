using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Archicad.Communication;

internal class ConnectionManager
{
  #region --- Fields ---

  private HttpClient HttpClient { get; set; }

  public static ConnectionManager Instance { get; } = new ConnectionManager();

  #endregion

  #region --- Functions ---

  public void Start(uint portNumber)
  {
    HttpClient = new HttpClient
    {
      BaseAddress = new System.Uri("http://127.0.0.1:" + portNumber),
      Timeout = TimeSpan.FromSeconds(300)
    };
  }

  public void Stop()
  {
    HttpClient?.CancelPendingRequests();
  }

  public async Task<string> Send(string message)
  {
    if (HttpClient is null)
    {
      throw new System.Exception("Connection is not started!");
    }

    HttpRequestMessage requestMessage = new() { Method = HttpMethod.Post, Content = new StringContent(message) };
    HttpResponseMessage responseMessage = await HttpClient.SendAsync(requestMessage);
    return await responseMessage.Content.ReadAsStringAsync();
  }

  #endregion
}
