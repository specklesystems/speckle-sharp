using Microsoft.Data.Sqlite;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace TestsPerformance;

public sealed class TestDataHelper : IDisposable
{
  private static readonly string BasePath = $"./temp {Guid.NewGuid()}";
  private const string ApplicationName = "Speckle Performance Tests";

  public SQLiteTransport Transport { get; private set; }
  public string ObjectId { get; private set; }

  public async Task SeedTransport(int dataComplexity)
  {
    Transport = new SQLiteTransport(BasePath, ApplicationName);

    //seed SQLite transport with test data
    StreamWrapper sw = new($"https://latest.speckle.dev/streams/efd2c6a31d/branches/{dataComplexity}");
    var acc = await sw.GetAccount().ConfigureAwait(false);
    using var client = new Client(acc);
    var branch = await client.BranchGet(sw.StreamId, sw.BranchName!, 1).ConfigureAwait(false);
    ObjectId = branch.commits.items[0].referencedObject;
    
    using ServerTransport remoteTransport = new(acc, sw.StreamId);
    Transport.BeginWrite();
    await remoteTransport.CopyObjectAndChildren(ObjectId, Transport).ConfigureAwait(false);
    await Transport.WriteComplete().ConfigureAwait(false);
  }

  public async Task<Base> DeserializeBase()
  {
    return await Speckle.Core.Api.Operations
        .Receive(ObjectId, null, Transport, onErrorAction: (message, ex) => throw new Exception(message, ex))
        .ConfigureAwait(false) ?? throw new InvalidOperationException("Test data was null");
  }

  public void Dispose()
  {
    Transport.Dispose();
    SqliteConnection.ClearAllPools();
    Directory.Delete(BasePath, true);
  }
}
