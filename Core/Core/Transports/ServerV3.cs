// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Net;
// using System.Net.Http;
// using System.Threading;
// using System.Threading.Tasks;
// using System.Timers;
// using Speckle.Core.Credentials;
// using Speckle.Core.Helpers;
// using Timer = System.Timers.Timer;
//
// namespace Speckle.Core.Transports;
//
// public class ServerV3 : ITransport, IDisposable
// {
//   public delegate ITransport Factory(Account account, string streamId, int timeoutSeconds, string? blobStorageFolder);
//   public string TransportName { get; set; } = "Remote Transport";
//
//   public string BaseUri { get; private set; }
//
//   public string StreamId { get; set; }
//
//   public Dictionary<string, object> TransportContext =>
//     new()
//     {
//       { "name", TransportName },
//       { "type", GetType().Name },
//       { "streamId", StreamId },
//       { "serverUrl", BaseUri }
//     };
//
//   public TimeSpan Elapsed { get; }
//   public int SavedObjectCount { get; }
//   public CancellationToken CancellationToken { get; set; }
//   public Action<string, int>? OnProgressAction { get; set; }
//   public Action<string, Exception>? OnErrorAction { get; set; }
//
//   public Account Account { get; set; }
//   public string ProjectId { get; set; }
//
//   public ServerV3(Account account, string projectId, int timeoutSeconds = 60, string? basePath = null)
//   {
//     Account = account;
//     ProjectId = projectId;
//     Client = Http.GetHttpProxyClient(
//       new SpeckleHttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip }
//     );
//
//     Client.BaseAddress = new Uri(Account.serverInfo.url);
//     Client.Timeout = new TimeSpan(0, 0, timeoutSeconds);
//
//     OldSchoolRequest = (HttpWebRequest)
//       //WebRequest.Create(new Uri($"https://directly-hardy-warthog.ngrok-free.app/objects/v3/{ProjectId}"));
//       WebRequest.Create(new Uri($"http://127.0.0.1:3000/objects/v3/{ProjectId}"));
//     OldSchoolRequest.Method = "POST";
//     OldSchoolRequest.ContentType = "application/octet-stream";
//     RequestStream = OldSchoolRequest.GetRequestStream();
//   }
//
//   public Stream RequestStream { get; set; }
//
//   public HttpWebRequest OldSchoolRequest { get; set; }
//
//   public SpeckleHttpContent SpeckleContent { get; set; }
//
//   public HttpRequestMessage Request { get; set; }
//
//   public StreamContent ContentStream { get; set; }
//
//   public MemoryStream MemoryStream { get; set; }
//
//   public HttpClient Client { get; set; }
//
//   public void BeginWrite()
//   {
//     // return;
//   }
//
//   public void EndWrite()
//   {
//     var pstt = _reqLen;
//     var pst = totalObjs;
//     RequestStream.Flush();
//     RequestStream.Close();
//     if (!hasSetupResponseWait)
//     {
//       setupResponseWait();
//     }
//     // RequestStream.Flush();
//     // RequestStream.Close();
//     // var response = OldSchoolRequest.GetResponse();
//     // var x = response;
//     // var xxx = OldSchoolRequest.BeginGetResponse(
//     //   (res) =>
//     //   {
//     //     _responseReady = true;
//     //   },
//     //   null
//     // );
//   }
//
//   private int _reqLen = 0;
//   private int totalObjs = 0;
//   private List<object> temp = new List<object>();
//
//   public void SaveObject(string id, string serializedObject)
//   {
//     totalObjs++;
//     temp.Add(serializedObject);
//     var payload = string.Join("\\t", new[] { id, serializedObject });
//     var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
//     RequestStream.Write(bytes, _reqLen, bytes.Length);
//     _reqLen += bytes.Length;
//   }
//
//   private bool hasSetupResponseWait = false;
//
//   private void setupResponseWait()
//   {
//     HttpWebResponse response = (HttpWebResponse)OldSchoolRequest.GetResponse();
//
//     using (Stream responseStream = response.GetResponseStream())
//     using (StreamReader reader = new StreamReader(responseStream))
//     {
//       Console.WriteLine(reader.ReadToEnd());
//     }
//
//     response.Close();
//
//     hasSetupResponseWait = true;
//   }
//
//   public void SaveObject(string id, ITransport sourceTransport) => throw new NotImplementedException();
//
//   public async Task WriteComplete()
//   {
//     // await OldSchoolRequest.GetResponseAsync().ConfigureAwait(false);
//     await Utilities
//       .WaitUntil(
//         () =>
//         {
//           return GetWriteCompletionStatus();
//         },
//         50
//       )
//       .ConfigureAwait(false);
//   }
//
//   private bool _responseReady = false;
//
//   public bool GetWriteCompletionStatus()
//   {
//     return _responseReady;
//   }
//
//   public string? GetObject(string id) => throw new NotImplementedException();
//
//   public Task<string> CopyObjectAndChildren(
//     string id,
//     ITransport targetTransport,
//     Action<int>? onTotalChildrenCountKnown = null
//   ) => throw new NotImplementedException();
//
//   public Task<Dictionary<string, bool>> HasObjects(IReadOnlyList<string> objectIds) =>
//     throw new NotImplementedException();
//
//   public void Dispose()
//   {
//     // TODO
//   }
// }
//
// public class SpeckleHttpContent : HttpContent
// {
//   private readonly Func<Stream, Task> _callback;
//
//   public SpeckleHttpContent(Func<Stream, Task> callback)
//   {
//     _callback = callback;
//   }
//
//   protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
//   {
//     return _callback(stream);
//   }
//
//   protected override bool TryComputeLength(out long length)
//   {
//     length = 0;
//     return false;
//   }
// }
