using System.Text;
using System.Text.Json;
using Speckle.BatchUploader.Sdk.CommunicationModels;

[assembly: CLSCompliant(true)]

namespace Speckle.BatchUploader.Sdk;

public sealed class BatchUploaderClient : IDisposable
{
  private readonly Uri _serverUri;
  private readonly HttpClient _httpClient;

  public BatchUploaderClient(Uri serverUri)
  {
    _serverUri = serverUri;

    using var p = System.Diagnostics.Process.GetCurrentProcess();
    _httpClient = new HttpClient();
    _httpClient.DefaultRequestHeaders.Add("pid", p.Id.ToString());
  }

  public async Task<Guid> AddJob(
    JobDescription description,
    string versionedHostAppName,
    CancellationToken cancellationToken = default
  )
  {
    using var requestMessage = new HttpRequestMessage(
      HttpMethod.Post,
      _serverUri + $"api/v1/Jobs?&versionedHostAppName={versionedHostAppName}"
    );
    requestMessage.Content = SerializeBody(description);
    using var response = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();
    var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
    return await JsonSerializer
      .DeserializeAsync<Guid>(stream, cancellationToken: cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task RegisterProcessor(string versionedHostAppName, CancellationToken cancellationToken = default)
  {
    using var requestMessage = new HttpRequestMessage(
      HttpMethod.Post,
      _serverUri + $"api/v1/Jobs/Processors?&versionedHostAppName={versionedHostAppName}"
    );
    using var response = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();
  }

  public async Task<Guid> GetJob(CancellationToken cancellationToken = default)
  {
    using var requestMessage = new HttpRequestMessage(HttpMethod.Get, _serverUri + $"api/v1/Jobs");
    using var response = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();
    var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
    return await JsonSerializer
      .DeserializeAsync<Guid>(stream, cancellationToken: cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<JobDescription> GetJobDescription(Guid jobId, CancellationToken cancellationToken = default)
  {
    using var requestMessage = new HttpRequestMessage(HttpMethod.Get, _serverUri + $"api/v1/Jobs/{jobId}/description");
    using var response = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();
    var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
    return await JsonSerializer
      .DeserializeAsync<JobDescription>(stream, cancellationToken: cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task<JobStatus> GetJobStatus(Guid jobId, CancellationToken cancellationToken = default)
  {
    using var requestMessage = new HttpRequestMessage(HttpMethod.Get, _serverUri + $"api/v1/Jobs/{jobId}/status");
    using var response = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();
    var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
    return await JsonSerializer
      .DeserializeAsync<JobStatus>(stream, cancellationToken: cancellationToken)
      .ConfigureAwait(false);
  }

  public async Task UpdateJobStatus(Guid jobId, JobStatus newStatus, CancellationToken cancellationToken = default)
  {
    using var requestMessage = new HttpRequestMessage(HttpMethod.Put, _serverUri + $"api/v1/Jobs/{jobId}/status");
    requestMessage.Content = SerializeBody(newStatus);
    using var response = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();
  }

  public async Task UpdateJobProgress(Guid jobId, JobProgress newStatus, CancellationToken cancellationToken = default)
  {
    using var requestMessage = new HttpRequestMessage(HttpMethod.Put, _serverUri + $"api/v1/Jobs/{jobId}/progress");
    requestMessage.Content = SerializeBody(newStatus);
    using var response = await _httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();
  }

  private static StringContent SerializeBody<T>(T value)
  {
    return new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
  }

  public void Dispose()
  {
    _httpClient.Dispose();
  }
}
