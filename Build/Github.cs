﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;

namespace Build;

public static class Github
{
  public static async Task BuildInstallers(string token, string runId)
  {
    using var client = new HttpClient();
    var payload = new { event_type = "build-installers", client_payload = new { run_id = runId } };
    var content = new StringContent(
      JsonSerializer.Serialize(payload),
      new MediaTypeHeaderValue(MediaTypeNames.Application.Json)
    );

    var request = new HttpRequestMessage()
    {
      Method = HttpMethod.Post,
      RequestUri = new Uri("https://api.github.com/repos/specklesystems/connector-installers/dispatches"),
      Headers =
      {
        Accept = { new MediaTypeWithQualityHeaderValue("application/vnd.github+json") },
        Authorization = new AuthenticationHeaderValue("Bearer", token),
        UserAgent = { new ProductInfoHeaderValue("Speckle.build", "3.0.0") }
      },
      Content = content
    };
    request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
    var response = await client.SendAsync(request).ConfigureAwait(false);
    if (!response.IsSuccessStatusCode)
    {
      throw new InvalidOperationException(
        $"{response.StatusCode} {response.ReasonPhrase} {await response.Content.ReadAsStringAsync().ConfigureAwait(false)}"
      );
    }
  }
}
