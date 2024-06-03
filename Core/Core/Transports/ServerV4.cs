using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace Speckle.Core.Transports;

public class ServerV4 : ITransport
{
  public string TransportName { get; set; } = "bla bla";
  public Dictionary<string, object> TransportContext =>
    new()
    {
      { "name", TransportName },
      { "type", GetType().Name },
      { "streamId", ProjectId },
      { "serverUrl", Account.serverInfo.url }
    };
  public TimeSpan Elapsed { get; }
  public int SavedObjectCount { get; set; }
  public CancellationToken CancellationToken { get; set; }
  public Action<string, int>? OnProgressAction { get; set; }
  public Action<string, Exception>? OnErrorAction { get; set; }

  public Account Account { get; set; }
  public string ProjectId { get; set; }
  public HttpClient Client { get; set; }

  private readonly StreamWriter _sw;
  private readonly string _basePath;
  private readonly string _basePathWithFile;
  private readonly CancellationToken _ct;
  private bool _hasFinishedUploadRequest;
  private bool _hasClosed;

  public ServerV4(Account account, string projectId, CancellationToken ct = default)
  {
    Account = account;
    ProjectId = projectId;

    Client = Http.GetHttpProxyClient(
      new SpeckleHttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip }
    );

    // Client.BaseAddress = new Uri(Account.serverInfo.url);
    Client.BaseAddress = new Uri("https://directly-hardy-warthog.ngrok-free.app");

    _basePath = Path.Combine(SpecklePathProvider.UserSpeckleFolderPath, "TransportV3aFiles");
    Directory.CreateDirectory(_basePath);
    _basePathWithFile = Path.Combine(_basePath, $"{ProjectId}-{Guid.NewGuid()}.jsonl");
    _sw = new StreamWriter(_basePathWithFile);
    _ct = ct;
  }

  public void BeginWrite() { }

  public void EndWrite()
  {
    if (_hasClosed)
    {
      return;
    }

    _hasClosed = true;
    _sw.Close();
    Upload();
  }

  public async void Upload()
  {
    using Stream fs = File.OpenRead(_basePathWithFile);
    using var multipart = new MultipartFormDataContent();
    using var gzip = new ServerUtils.GzipContent(new StreamContent(fs));
    gzip.Headers.ContentType = new MediaTypeHeaderValue("application/gzip");
    multipart.Add(gzip, "file", ProjectId);
    try
    {
      var response = await Client
        .PostAsync(new Uri($"/objects/v4/{ProjectId}", UriKind.Relative), multipart, _ct)
        .ConfigureAwait(false);
      response.EnsureSuccessStatusCode();
    }
    catch (Exception e)
    {
      var p = e;
    }
    finally
    {
      //File.Delete(_basePathWithFile);
      _hasFinishedUploadRequest = true;
    }
  }

  public void SaveObject(string id, string serializedObject)
  {
    _sw.WriteLine(serializedObject);
    SavedObjectCount++;
  }

  public void SaveObject(string id, ITransport sourceTransport) => throw new NotImplementedException();

  public async Task WriteComplete() =>
    await Utilities.WaitUntil(() => _hasFinishedUploadRequest, 50).ConfigureAwait(false);

  public string? GetObject(string id) => throw new NotImplementedException();

  public Task<string> CopyObjectAndChildren(
    string id,
    ITransport targetTransport,
    Action<int>? onTotalChildrenCountKnown = null
  ) => throw new NotImplementedException();

  public Task<Dictionary<string, bool>> HasObjects(IReadOnlyList<string> objectIds) =>
    throw new NotImplementedException();
}
