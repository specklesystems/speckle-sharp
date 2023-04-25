using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using MongoDB.Bson;
using MongoDB.Driver;
using Speckle.Core.Logging;
using Timer = System.Timers.Timer;

namespace Speckle.Core.Transports;

// If data storage accessed by transports will always use the hash and content field names, move this enum to ITransport instead.
public enum Field
{
  hash,
  content
}

// Question: the benefit of noSQL is the use of unstructured collections of variable documents.
// Explore storing partially serialized Speckle objects with dynamically generated fields instead of just a content string?
public class MongoDBTransport : IDisposable, ITransport
{
  private bool IS_WRITING;
  private int MAX_TRANSACTION_SIZE = 1000;
  private int PollInterval = 500;

  private ConcurrentQueue<(string, string, int)> Queue = new();

  /// <summary>
  /// Timer that ensures queue is consumed if less than MAX_TRANSACTION_SIZE objects are being sent.
  /// </summary>
  /// Is this to prevent requests to read an object before it is written, or to handle read/write locks?
  /// If this is can differ per transport, better to use Database.currentOp() to determine if write operations are waiting for a lock.
  private Timer WriteTimer;

  public MongoDBTransport(
    string connectionString = "mongodb://localhost:27017",
    string applicationName = "Speckle",
    string scope = "Objects"
  )
  {
    SpeckleLog.Logger.Information("Creating new MongoDB Transport");

    ConnectionString = connectionString;
    Client = new MongoClient(ConnectionString);
    Database = (MongoDatabaseBase)Client.GetDatabase(applicationName);
    Collection = Database.GetCollection<BsonDocument>(scope);

    Initialize();

    WriteTimer = new Timer
    {
      AutoReset = true,
      Enabled = false,
      Interval = PollInterval
    };
    WriteTimer.Elapsed += WriteTimerElapsed;
  }

  public string ConnectionString { get; set; }

  private MongoClient Client { get; set; }
  private IMongoDatabase Database { get; set; }
  private IMongoCollection<BsonDocument> Collection { get; set; }

  public void Dispose()
  {
    // MongoDB collection connection should dispose automatically

    // Time out locking could be added if an expected use case is multiple clients writing to the same server
  }

  public string TransportName { get; set; } = "MongoTransport";

  public Dictionary<string, object> TransportContext => new() { { "name", TransportName }, { "type", GetType().Name } };

  public CancellationToken CancellationToken { get; set; }

  public Action<string, int> OnProgressAction { get; set; }

  public Action<string, Exception> OnErrorAction { get; set; }
  public int SavedObjectCount { get; private set; }

  // not implementing this properly
  public TimeSpan Elapsed => TimeSpan.Zero;

  public void BeginWrite()
  {
    SavedObjectCount = 0;
  }

  public void EndWrite() { }

  public Task<Dictionary<string, bool>> HasObjects(List<string> objectIds)
  {
    throw new NotImplementedException();
  }

  private void Initialize()
  {
    // Assumes mongoDB server is running
    // Mongo database and collection should be created automatically if it doesn't already exist

    // Check if the connection is successful
    bool isMongoLive = Database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);
    if (!isMongoLive)
      OnErrorAction(TransportName, new Exception("The Mongo database could not be reached."));
  }

  /// <summary>
  /// Returns all the objects in the store.
  /// </summary>
  /// <returns></returns>
  internal IEnumerable<string> GetAllObjects()
  {
    var documents = Collection.Find(new BsonDocument()).ToList();
    List<string> documentContents = new();
    foreach (BsonDocument document in documents)
      documentContents.Add(document[Field.content.ToString()].AsString);
    return documentContents;
  }

  /// <summary>
  /// Deletes an object. Note: do not use for any speckle object transport, as it will corrupt the database.
  /// </summary>
  /// <param name="hash"></param>
  internal void DeleteObject(string hash)
  {
    var filter = Builders<BsonDocument>.Filter.Eq(Field.hash.ToString(), hash);
    Collection.DeleteOne(filter);
  }

  #region Writes

  /// <summary>
  /// Awaits until write completion (ie, the current queue is fully consumed).
  /// </summary>
  /// <returns></returns>
  public async Task WriteComplete()
  {
    await Utilities
      .WaitUntil(
        () =>
        {
          return GetWriteCompletionStatus();
        },
        500
      )
      .ConfigureAwait(false);
  }

  /// <summary>
  /// Returns true if the current write queue is empty and committed.
  /// </summary>
  /// <returns></returns>
  /// <remarks>
  /// Mongo has intent shared and intent exclusive client operations.
  /// Each category shares a lock, with intent exclusive operations prioritized.
  /// Would change to Database.currentOp() to determine if write operations are waiting for a lock, if the WriteTimer is deprecated
  /// </remarks>
  public bool GetWriteCompletionStatus()
  {
    Console.WriteLine($"write completion {Queue.Count == 0 && !IS_WRITING}");
    return Queue.Count == 0 && !IS_WRITING;
  }

  private void WriteTimerElapsed(object sender, ElapsedEventArgs e)
  {
    WriteTimer.Enabled = false;
    if (!IS_WRITING && Queue.Count != 0)
      ConsumeQueue();
  }

  private void ConsumeQueue()
  {
    IS_WRITING = true;
    var i = 0;
    ValueTuple<string, string, int> result;

    while (i < MAX_TRANSACTION_SIZE && Queue.TryPeek(out result))
    {
      Queue.TryDequeue(out result);
      var document = new BsonDocument
      {
        { Field.hash.ToString(), result.Item1 },
        { Field.content.ToString(), result.Item2 }
      };
      Collection.InsertOne(document);
    }

    if (Queue.Count > 0)
      ConsumeQueue();

    IS_WRITING = false;
  }

  /// <summary>
  /// Adds an object to the saving queue.
  /// </summary>
  /// <param name="hash"></param>
  /// <param name="serializedObject"></param>
  public void SaveObject(string hash, string serializedObject)
  {
    Queue.Enqueue((hash, serializedObject, Encoding.UTF8.GetByteCount(serializedObject)));

    WriteTimer.Enabled = true;
    WriteTimer.Start();
  }

  public void SaveObject(string hash, ITransport sourceTransport)
  {
    var serializedObject = sourceTransport.GetObject(hash);
    Queue.Enqueue((hash, serializedObject, Encoding.UTF8.GetByteCount(serializedObject)));
  }

  /// <summary>
  /// Directly saves the object in the db.
  /// </summary>
  /// <param name="hash"></param>
  /// <param name="serializedObject"></param>
  public void SaveObjectSync(string hash, string serializedObject)
  {
    var document = new BsonDocument { { Field.hash.ToString(), hash }, { Field.content.ToString(), serializedObject } };
    Collection.InsertOne(document);
  }

  #endregion

  #region Reads

  /// <summary>
  /// Gets an object.
  /// </summary>
  /// <param name="hash"></param>
  /// <returns></returns>
  public string GetObject(string hash)
  {
    var filter = Builders<BsonDocument>.Filter.Eq(Field.hash.ToString(), hash);
    BsonDocument objectDocument = Collection.Find(filter).FirstOrDefault();
    if (objectDocument != null)
      return objectDocument[Field.content.ToString()].AsString;

    // pass on the duty of null checks to consumers
    return null;
  }

  public async Task<string> CopyObjectAndChildren(
    string hash,
    ITransport targetTransport,
    Action<int> onTotalChildrenCountKnown = null
  )
  {
    throw new NotImplementedException();
  }

  #endregion
}
