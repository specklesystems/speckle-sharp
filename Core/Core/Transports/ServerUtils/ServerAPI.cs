using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Helpers;
using Speckle.Core.Models;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Linq;

namespace Speckle.Core.Transports.ServerUtils;

public class ServerApi : IDisposable, IServerApi
{
  private string BaseUri;
  private int BATCH_SIZE_GET_OBJECTS = 10000;
  private int BATCH_SIZE_HAS_OBJECTS = 100000;

  private HttpClient Client;
  private int MAX_MULTIPART_COUNT = 5;
  private int MAX_MULTIPART_SIZE = 25_000_000;
  private int MAX_OBJECT_SIZE = 25_000_000;
  private int MAX_REQUEST_SIZE = 100_000_000;
  private HashSet<int> RETRY_CODES = new() { 408, 502, 503, 504 };
  private int RETRY_COUNT = 3;

  public ServerApi(string baseUri, string authorizationToken, string blobStorageFolder, int timeoutSeconds = 60)
  {
    BaseUri = baseUri;
    CancellationToken = CancellationToken.None;

    BlobStorageFolder = blobStorageFolder;

    Client = Http.GetHttpProxyClient(
      new SpeckleHttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip }
    );

    Client.BaseAddress = new Uri(baseUri);
    Client.Timeout = new TimeSpan(0, 0, timeoutSeconds);

    if (authorizationToken.ToLowerInvariant().Contains("bearer"))
      Client.DefaultRequestHeaders.Add("Authorization", authorizationToken);
    else
      Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authorizationToken}");
  }

  private int RetriedCount { get; set; }
  public CancellationToken CancellationToken { get; set; }
  public bool CompressPayloads { get; set; } = true;

  public string BlobStorageFolder { get; set; }

  /// <summary>
  /// Callback when sending batches. Parameters: object count, total bytes sent
  /// </summary>
  public Action<int, int> OnBatchSent { get; set; }

  public void Dispose()
  {
    Client.Dispose();
  }

  public async Task<string> DownloadSingleObject(string streamId, string objectId)
  {
    CancellationToken.ThrowIfCancellationRequested();

    // Get root object
    using var rootHttpMessage = new HttpRequestMessage
    {
      RequestUri = new Uri($"/objects/{streamId}/{objectId}/single", UriKind.Relative),
      Method = HttpMethod.Get
    };

    HttpResponseMessage rootHttpResponse = null;
    while (ShouldRetry(rootHttpResponse))
      rootHttpResponse = await Client
        .SendAsync(rootHttpMessage, HttpCompletionOption.ResponseContentRead, CancellationToken)
        .ConfigureAwait(false);
    rootHttpResponse.EnsureSuccessStatusCode();

    string rootObjectStr = await rootHttpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
    return rootObjectStr;
  }

  public async Task DownloadObjects(string streamId, List<string> objectIds, CbObjectDownloaded onObjectCallback)
  {
    if (objectIds.Count == 0)
      return;
    if (objectIds.Count < BATCH_SIZE_GET_OBJECTS)
    {
      await DownloadObjectsImpl(streamId, objectIds, onObjectCallback).ConfigureAwait(false);
      return;
    }

    List<string> crtRequest = new();
    foreach (string id in objectIds)
    {
      if (crtRequest.Count >= BATCH_SIZE_GET_OBJECTS)
      {
        await DownloadObjectsImpl(streamId, crtRequest, onObjectCallback).ConfigureAwait(false);
        crtRequest = new List<string>();
      }
      crtRequest.Add(id);
    }
    await DownloadObjectsImpl(streamId, crtRequest, onObjectCallback).ConfigureAwait(false);
  }

  public async Task<Dictionary<string, bool>> HasObjects(string streamId, List<string> objectIds)
  {
    if (objectIds.Count <= BATCH_SIZE_HAS_OBJECTS)
      return await HasObjectsImpl(streamId, objectIds).ConfigureAwait(false);

    Dictionary<string, bool> ret = new();
    List<string> crtBatch = new(BATCH_SIZE_HAS_OBJECTS);
    foreach (string objectId in objectIds)
    {
      crtBatch.Add(objectId);
      if (crtBatch.Count >= BATCH_SIZE_HAS_OBJECTS)
      {
        Dictionary<string, bool> batchResult = await HasObjectsImpl(streamId, crtBatch).ConfigureAwait(false);
        foreach (KeyValuePair<string, bool> kv in batchResult)
          ret[kv.Key] = kv.Value;
        crtBatch = new List<string>(BATCH_SIZE_HAS_OBJECTS);
      }
    }
    if (crtBatch.Count > 0)
    {
      Dictionary<string, bool> batchResult = await HasObjectsImpl(streamId, crtBatch).ConfigureAwait(false);
      foreach (KeyValuePair<string, bool> kv in batchResult)
        ret[kv.Key] = kv.Value;
    }
    return ret;
  }

  public async Task UploadObjects(string streamId, List<(string, string)> objects)
  {
    if (objects.Count == 0)
      return;

    // 1. Split into parts of MAX_MULTIPART_SIZE size (can be exceptions until a max of MAX_OBJECT_SIZE if a single obj is larger than MAX_MULTIPART_SIZE)
    List<List<(string, string)>> multipartedObjects = new();
    List<int> multipartedObjectsSize = new();

    List<(string, string)> crtMultipart = new();
    int crtMultipartSize = 0;

    foreach ((string id, string json) in objects)
    {
      int objSize = Encoding.UTF8.GetByteCount(json);
      if (objSize > MAX_OBJECT_SIZE)
        throw new Exception(
          $"Object too large (size {objSize}, max size {MAX_OBJECT_SIZE}). Consider using detached/chunked properties"
        );

      if (crtMultipartSize + objSize <= MAX_MULTIPART_SIZE)
      {
        crtMultipart.Add((id, json));
        crtMultipartSize += objSize;
        continue;
      }

      // new multipart
      if (crtMultipart.Count > 0)
      {
        multipartedObjects.Add(crtMultipart);
        multipartedObjectsSize.Add(crtMultipartSize);
      }
      crtMultipart = new List<(string, string)>();
      crtMultipart.Add((id, json));
      crtMultipartSize = objSize;
    }
    multipartedObjects.Add(crtMultipart);
    multipartedObjectsSize.Add(crtMultipartSize);

    // 2. Split multiparts into individual server requests of max size MAX_REQUEST_SIZE or max length MAX_MULTIPART_COUNT and send them
    List<List<(string, string)>> crtRequest = new();
    int crtRequestSize = 0;
    int crtObjectCount = 0;
    for (int i = 0; i < multipartedObjects.Count; i++)
    {
      List<(string, string)> multipart = multipartedObjects[i];
      int multipartSize = multipartedObjectsSize[i];
      if (crtRequestSize + multipartSize > MAX_REQUEST_SIZE || crtRequest.Count >= MAX_MULTIPART_COUNT)
      {
        await UploadObjectsImpl(streamId, crtRequest).ConfigureAwait(false);
        OnBatchSent?.Invoke(crtObjectCount, crtRequestSize);
        crtRequest = new List<List<(string, string)>>();
        crtRequestSize = 0;
        crtObjectCount = 0;
      }
      crtRequest.Add(multipart);
      crtRequestSize += multipartSize;
      crtObjectCount += multipart.Count;
    }
    if (crtRequest.Count > 0)
    {
      await UploadObjectsImpl(streamId, crtRequest).ConfigureAwait(false);
      OnBatchSent?.Invoke(crtObjectCount, crtRequestSize);
    }
  }

  public async Task UploadBlobs(string streamId, List<(string, string)> blobs)
  {
    CancellationToken.ThrowIfCancellationRequested();
    if (blobs.Count == 0)
      return;

    var multipartFormDataContent = new MultipartFormDataContent();
    var streams = new List<Stream>();
    foreach (var (id, filePath) in blobs)
    {
      var fileName = Path.GetFileName(filePath);
      var stream = File.OpenRead(filePath);
      streams.Add(stream);
      var fsc = new StreamContent(stream);
      var hash = id.Split(':')[1];

      multipartFormDataContent.Add(fsc, $"hash:{hash}", fileName);
    }

    using var message = new HttpRequestMessage
    {
      RequestUri = new Uri($"/api/stream/{streamId}/blob", UriKind.Relative),
      Method = HttpMethod.Post,
      Content = multipartFormDataContent
    };

    try
    {
      HttpResponseMessage response = null;
      while (ShouldRetry(response)) //TODO: can we get rid of this now we have polly?
        response = await Client.SendAsync(message, CancellationToken).ConfigureAwait(false);
      response.EnsureSuccessStatusCode();

      foreach (var stream in streams)
        stream.Dispose();
    }
    catch (Exception ex)
    {
      foreach (var stream in streams)
        stream.Dispose();
      throw;
    }
  }

  public async Task DownloadBlobs(string streamId, List<string> blobIds, CbBlobdDownloaded onBlobDownloaded)
  {
    foreach (var blobId in blobIds)
      try
      {
        using var blobMessage = new HttpRequestMessage
        {
          RequestUri = new Uri($"api/stream/{streamId}/blob/{blobId}", UriKind.Relative),
          Method = HttpMethod.Get
        };

        var response = await Client.SendAsync(blobMessage, CancellationToken).ConfigureAwait(false);
        IEnumerable<string> cdHeaderValues;
        response.Content.Headers.TryGetValues("Content-Disposition", out cdHeaderValues);

        var cdHeader = cdHeaderValues.First();
        var fileName = cdHeader.Split(new[] { "filename=" }, StringSplitOptions.None)[1].TrimStart('"').TrimEnd('"');

        string fileLocation = Path.Combine(
          BlobStorageFolder,
          $"{blobId.Substring(0, Blob.LocalHashPrefixLength)}-{fileName}"
        );
        using (var fs = new FileStream(fileLocation, FileMode.OpenOrCreate))
          await response.Content.CopyToAsync(fs).ConfigureAwait(false);

        response.Dispose();
        onBlobDownloaded();
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to download blob {blobId}", ex);
      }
  }

  private async Task DownloadObjectsImpl(string streamId, List<string> objectIds, CbObjectDownloaded onObjectCallback)
  {
    // Stopwatch sw = new Stopwatch(); sw.Start();

    CancellationToken.ThrowIfCancellationRequested();

    using var childrenHttpMessage = new HttpRequestMessage
    {
      RequestUri = new Uri($"/api/getobjects/{streamId}", UriKind.Relative),
      Method = HttpMethod.Post
    };

    Dictionary<string, string> postParameters = new();
    postParameters.Add("objects", JsonConvert.SerializeObject(objectIds));
    string serializedPayload = JsonConvert.SerializeObject(postParameters);
    childrenHttpMessage.Content = new StringContent(serializedPayload, Encoding.UTF8, "application/json");
    childrenHttpMessage.Headers.Add("Accept", "text/plain");

    HttpResponseMessage childrenHttpResponse = null;
    while (ShouldRetry(childrenHttpResponse))
      childrenHttpResponse = await Client
        .SendAsync(childrenHttpMessage, HttpCompletionOption.ResponseHeadersRead, CancellationToken)
        .ConfigureAwait(false);
    childrenHttpResponse.EnsureSuccessStatusCode();

    Stream childrenStream = await childrenHttpResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);

    using (childrenStream)
    using (var reader = new StreamReader(childrenStream, Encoding.UTF8))
    {
      string line;
      while ((line = reader.ReadLine()) != null)
      {
        CancellationToken.ThrowIfCancellationRequested();

        var pcs = line.Split(new[] { '\t' }, 2);
        onObjectCallback(pcs[0], pcs[1]);
      }
    }

    // Console.WriteLine($"ServerApi::DownloadObjects({objectIds.Count}) request in {sw.ElapsedMilliseconds / 1000.0} sec");
  }

  private async Task<Dictionary<string, bool>> HasObjectsImpl(string streamId, List<string> objectIds)
  {
    CancellationToken.ThrowIfCancellationRequested();

    // Stopwatch sw = new Stopwatch(); sw.Start();

    string objectsPostParameter = JsonConvert.SerializeObject(objectIds);
    var payload = new Dictionary<string, string> { { "objects", objectsPostParameter } };
    string serializedPayload = JsonConvert.SerializeObject(payload);
    var uri = new Uri($"/api/diff/{streamId}", UriKind.Relative);
    HttpResponseMessage response = null;
    using StringContent stringContent = new(serializedPayload, Encoding.UTF8, "application/json");
    while (ShouldRetry(response))
      response = await Client.PostAsync(uri, stringContent, CancellationToken).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();

    var hasObjectsJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    Dictionary<string, bool> hasObjects = new();

    JObject doc = JObject.Parse(hasObjectsJson);
    foreach (KeyValuePair<string, JToken> prop in doc)
      hasObjects[prop.Key] = (bool)prop.Value;

    // Console.WriteLine($"ServerApi::HasObjects({objectIds.Count}) request in {sw.ElapsedMilliseconds / 1000.0} sec");

    return hasObjects;
  }

  private async Task UploadObjectsImpl(string streamId, List<List<(string, string)>> multipartedObjects)
  {
    // Stopwatch sw = new Stopwatch(); sw.Start();

    CancellationToken.ThrowIfCancellationRequested();

    using var message = new HttpRequestMessage
    {
      RequestUri = new Uri($"/objects/{streamId}", UriKind.Relative),
      Method = HttpMethod.Post
    };

    var multipart = new MultipartFormDataContent();

    int mpId = 0;
    foreach (List<(string, string)> mpData in multipartedObjects)
    {
      mpId++;

      var _ctBuilder = new StringBuilder("[");
      for (int i = 0; i < mpData.Count; i++)
      {
        if (i > 0)
          _ctBuilder.Append(",");
        _ctBuilder.Append(mpData[i].Item2);
      }
      _ctBuilder.Append("]");
      string _ct = _ctBuilder.ToString();

      if (CompressPayloads)
      {
        var content = new GzipContent(new StringContent(_ct, Encoding.UTF8));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/gzip");
        multipart.Add(content, $"batch-{mpId}", $"batch-{mpId}");
      }
      else
      {
        multipart.Add(new StringContent(_ct, Encoding.UTF8), $"batch-{mpId}", $"batch-{mpId}");
      }
    }
    message.Content = multipart;
    HttpResponseMessage response = null;
    while (ShouldRetry(response))
      response = await Client.SendAsync(message, CancellationToken).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();

    // Console.WriteLine($"ServerApi::UploadObjects({totalObjCount}) request in {sw.ElapsedMilliseconds / 1000.0} sec");
  }

  public async Task<List<string>> HasBlobs(string streamId, List<string> blobIds)
  {
    CancellationToken.ThrowIfCancellationRequested();

    var payload = JsonConvert.SerializeObject(blobIds);
    var uri = new Uri($"/api/stream/{streamId}/blob/diff", UriKind.Relative);

    HttpResponseMessage response = null;
    using StringContent stringContent = new(payload, Encoding.UTF8, "application/json");
    //TODO: can we get rid of this now we have polly?
    while (ShouldRetry(response))
      response = await Client.PostAsync(uri, stringContent, CancellationToken).ConfigureAwait(false);

    response.EnsureSuccessStatusCode();

    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    var parsed = JsonConvert.DeserializeObject<List<string>>(responseString);
    return parsed;
  }

  //TODO: can we get rid of this now we have polly?
  private bool ShouldRetry(HttpResponseMessage serverResponse)
  {
    if (serverResponse == null)
      return true;
    if (!RETRY_CODES.Contains((int)serverResponse.StatusCode))
      return false;
    if (RetriedCount >= RETRY_COUNT)
      return false;
    RetriedCount += 1;
    return true;
  }

  private class BlobUploadResult
  {
    public List<BlobUploadResultItem> uploadResults { get; set; }
  }

  private class BlobUploadResultItem
  {
    public string blobId { get; set; }
    public string formKey { get; set; }
    public string fileName { get; set; }
  }
}
