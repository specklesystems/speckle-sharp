using Microsoft.Data.Sqlite;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.Core.Tests.Performance;

public sealed class TestDataHelper : IDisposable
{
  private static readonly string s_basePath = $"./temp {Guid.NewGuid()}";
  private const string APPLICATION_NAME = "Speckle Performance Tests";

  public SQLiteTransport Transport { get; private set; }
  public string ObjectId { get; private set; }

  public async Task SeedTransport(int dataComplexity)
  {
    Transport = new SQLiteTransport(s_basePath, APPLICATION_NAME);

    //seed SQLite transport with test data
    ObjectId = await SeedTransport(dataComplexity, Transport).ConfigureAwait(false);
  }

  public static async Task<string> SeedTransport(int dataComplexity, ITransport transport)
  {
    //seed SQLite transport with test data
    StreamWrapper sw = new($"https://latest.speckle.dev/streams/efd2c6a31d/branches/{dataComplexity}");
    var acc = await sw.GetAccount().ConfigureAwait(false);
    using var client = new Client(acc);
    var branch = await client.BranchGet(sw.StreamId, sw.BranchName!, 1).ConfigureAwait(false);
    var objectId = branch!.commits.items[0].referencedObject;

    using ServerTransport remoteTransport = new(acc, sw.StreamId);
    transport.BeginWrite();
    await remoteTransport.CopyObjectAndChildren(objectId, transport).ConfigureAwait(false);
    transport.EndWrite();
    await transport.WriteComplete().ConfigureAwait(false);

    return objectId;
  }

  public async Task<Base> DeserializeBase()
  {
    return await Operations.Receive(ObjectId, null, Transport).ConfigureAwait(false);
  }

  public void Dispose()
  {
    Transport.Dispose();
    SqliteConnection.ClearAllPools();
    Directory.Delete(s_basePath, true);
  }
}
