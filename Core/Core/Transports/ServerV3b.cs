using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;

namespace Speckle.Core.Transports;

public class ServerV3b : ITransport
{
  public string TransportName { get; set; } = "bla bla 2";
  public Dictionary<string, object> TransportContext =>
    new()
    {
      { "name", TransportName },
      { "type", GetType().Name },
      { "streamId", ProjectId },
      { "serverUrl", Account.serverInfo.url }
    };
  public TimeSpan Elapsed { get; }
  public int SavedObjectCount { get; }
  public CancellationToken CancellationToken { get; set; }
  public Action<string, int>? OnProgressAction { get; set; }
  public Action<string, Exception>? OnErrorAction { get; set; }

  public Account Account { get; set; }
  public string ProjectId { get; set; }

  private readonly NetworkStream _stream;
  private readonly TcpClient tcpClient;

  public ServerV3b(Account account, string projectId)
  {
    Account = account;
    ProjectId = projectId;

    var uri = new Uri("https://directly-hardy-warthog.ngrok-free.app/objects/v3/pasta");

    tcpClient = new TcpClient();
    tcpClient.Connect(uri.Host, uri.Port);
    _stream = tcpClient.GetStream();
  }

  public void BeginWrite() { }

  public void EndWrite()
  {
    // Is this the moment we can close things up? or wait for them to close up?
    // _stream.Close();
    // tcpClient.Close();
  }

  private int _requestLength;

  public void SaveObject(string id, string serializedObject)
  {
    var payload = string.Join("\\t", new[] { id, serializedObject });
    var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
    _stream.Write(bytes, _requestLength, bytes.Length);
    _requestLength += bytes.Length;
  }

  public void SaveObject(string id, ITransport sourceTransport) => throw new NotImplementedException();

  public Task WriteComplete()
  {
    return Utilities.WaitUntil(
      () =>
      {
        return false;
      },
      100
    );
  }

  public string? GetObject(string id) => throw new NotImplementedException();

  public Task<string> CopyObjectAndChildren(
    string id,
    ITransport targetTransport,
    Action<int>? onTotalChildrenCountKnown = null
  ) => throw new NotImplementedException();

  public Task<Dictionary<string, bool>> HasObjects(IReadOnlyList<string> objectIds) =>
    throw new NotImplementedException();
}
