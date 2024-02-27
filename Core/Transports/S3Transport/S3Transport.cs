using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Speckle.Core.Logging;
using Timer = System.Timers.Timer;

namespace Speckle.Core.Transports;

public class S3Transport : ITransport, IDisposable
{
  private const int MAX_TRANSACTION_SIZE = 1000;
  private const int POLL_INTERVAL = 500;
  private bool _isWriting;
  private bool _isDisposed;

  private readonly AmazonS3Client _amazonS3Client;
  private readonly string _bucket;
  private readonly string _path;

  private readonly ConcurrentQueue<(string, string)> _queue = new();

  /// <summary>
  /// Timer that ensures queue is consumed if less than MAX_TRANSACTION_SIZE objects are being sent.
  /// </summary>
  /// Is this to prevent requests to read an object before it is written, or to handle read/write locks?
  /// If this is can differ per transport, better to use Database.currentOp() to determine if write operations are waiting for a lock.
  private readonly Timer _writeTimer;

  public S3Transport(
    string? accessKey = null,
    string? secretKey = null,
    string? region = null,
    string? bucketName = null,
    string? path = null
  )
  {
    SpeckleLog.Logger.Information("Creating new S3 Transport");

    var clientConfig = new AmazonS3Config
    {
      RegionEndpoint = AwsCredentials.GetRegionEndpoint(region),
      RetryMode = RequestRetryMode.Standard
    };
    var credentials =
      AwsCredentials.GetCredentials(accessKey, secretKey) ?? FallbackCredentialsFactory.GetCredentials();
    _amazonS3Client = new AmazonS3Client(credentials, clientConfig);
    _bucket =
      bucketName
      ?? Environment.GetEnvironmentVariable("BUCKET_NAME")
      ?? throw new InvalidOperationException("Bucket name not provided");
    _path =
      path
      ?? Environment.GetEnvironmentVariable("BUCKET_PATH")
      ?? throw new InvalidOperationException("Path name not provided");

    _writeTimer = new Timer
    {
      AutoReset = true,
      Enabled = false,
      Interval = POLL_INTERVAL
    };
    _writeTimer.Elapsed += WriteTimerElapsed;
  }

  public string TransportName { get; set; } = "S3Transport";

  public Dictionary<string, object> TransportContext => new() { { "name", TransportName }, { "type", GetType().Name } };
  public TimeSpan Elapsed => TimeSpan.Zero;
  public int SavedObjectCount { get; private set; }
  public CancellationToken CancellationToken { get; set; }
  public Action<string, int>? OnProgressAction { get; set; }
  public Action<string, Exception>? OnErrorAction { get; set; }

  public void BeginWrite() => SavedObjectCount = 0;

  public void EndWrite() { }

  public void SaveObject(string id, string serializedObject)
  {
    _queue.Enqueue((id, serializedObject));

    _writeTimer.Enabled = true;
    _writeTimer.Start();
  }

  public void SaveObject(string id, ITransport sourceTransport)
  {
    var serializedObject = sourceTransport.GetObject(id);
    if (serializedObject is not null)
    {
      SaveObject(id, serializedObject);
    }
  }

  public async Task WriteComplete()
  {
    await Utilities
      .WaitUntil(
        () =>
        {
          Console.WriteLine($"write completion {_queue.IsEmpty && !_isWriting}");
          return _queue.IsEmpty && !_isWriting;
        },
        500
      )
      .ConfigureAwait(false);
  }

  public string? GetObject(string id)
  {
    var response = _amazonS3Client
      .GetObjectAsync(_bucket, MakeKey(id), CancellationToken)
      .ConfigureAwait(false)
      .GetAwaiter()
      .GetResult();
    using (response)
    using (var stream = response.ResponseStream)
    using (var reader = new StreamReader(stream))
    {
      return reader.ReadToEnd();
    }
  }

  public Task<string> CopyObjectAndChildren(
    string id,
    ITransport targetTransport,
    Action<int>? onTotalChildrenCountKnown = null
  ) => throw new NotImplementedException();

  public Task<Dictionary<string, bool>> HasObjects(IReadOnlyList<string> objectIds) =>
    throw new NotImplementedException();

  public override string ToString() => $"S3 Transport @{_bucket}/{_path}";

  private string MakeKey(string key) => $"{_path}/{key}";

  private async void WriteTimerElapsed(object sender, ElapsedEventArgs e)
  {
    //this should be enough to prevent re-entrancy
    _writeTimer.Enabled = false;
    if (!_isWriting && !_queue.IsEmpty)
    {
      await ConsumeQueue().ConfigureAwait(false);
    }
  }

  private async Task ConsumeQueue()
  {
    if (_isDisposed)
    {
      return;
    }
    _isWriting = true;
    var i = 0;

    while (i < MAX_TRANSACTION_SIZE && _queue.TryDequeue(out var result))
    {
      await _amazonS3Client
        .PutObjectAsync(
          new PutObjectRequest()
          {
            BucketName = _bucket,
            Key = MakeKey(result.Item1),
            ContentBody = result.Item2,
            ContentType = "application/json"
          },
          CancellationToken
        )
        .ConfigureAwait(false);

      i++;
    }

    if (i < MAX_TRANSACTION_SIZE && !_queue.IsEmpty)
    {
      await ConsumeQueue().ConfigureAwait(false);
    }

    _isWriting = false;
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  ~S3Transport()
  {
    Dispose(false);
  }

  protected virtual void Dispose(bool isDisposing)
  {
    if (isDisposing)
    {
      _isDisposed = false;
      _writeTimer.Enabled = false;
      _writeTimer.Dispose();
      _amazonS3Client.Dispose();
    }
  }
}
