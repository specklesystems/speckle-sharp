// See https://aka.ms/new-console-template for more information

using Speckle.Core.Credentials;
using Speckle.Core.Helpers;

const string FILE_PATH = "C:\\Users\\Jedd\\Downloads\\important file.png";
const string PROJECT_ID = "c1faab5c62";
const string SERVER_URL = "https://app.speckle.systems";

var acc = AccountManager.GetAccounts(SERVER_URL).First();
using var httpClient = Http.GetHttpProxyClient();
Http.AddAuthHeader(httpClient, acc.token);

var fileStream = new FileStream(FILE_PATH, FileMode.Open, FileAccess.Read);
using StreamContent streamContent = new(fileStream);
using MultipartFormDataContent formData = new() { { streamContent, "files", Path.GetFileName(FILE_PATH) } };

var response = await httpClient
  .PostAsync(new Uri($"{SERVER_URL}/api/file/ifc/{PROJECT_ID}"), formData)
  .ConfigureAwait(false);

Console.WriteLine($"Response code: {response.StatusCode}");
Console.WriteLine($"Response content: {await response.Content.ReadAsStringAsync().ConfigureAwait(false)}");
// Console.WriteLine($"Response headers: {response.Headers}");
