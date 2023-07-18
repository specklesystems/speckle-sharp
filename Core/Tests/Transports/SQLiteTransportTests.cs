using Microsoft.Data.Sqlite;
using NUnit.Framework;
using Speckle.Core.Transports;

namespace TestsUnit.Transports;

[TestFixture]
[TestOf(nameof(SQLiteTransport))]
public sealed class SQLiteTransportTests : TransportTests, IDisposable
{
  protected override ITransport Sut => _sqlite!;

  private SQLiteTransport? _sqlite;

  private static readonly string BasePath = $"./temp {Guid.NewGuid()}";
  private const string ApplicationName = "Speckle Integration Tests";

  [SetUp]
  public void Setup()
  {
    _sqlite = new SQLiteTransport(BasePath, ApplicationName);
  }

  [TearDown]
  public void TearDown()
  {
    this.Dispose();
    SqliteConnection.ClearAllPools();
    Directory.Delete(BasePath, true);
    _sqlite = null;
  }

  [Test]
  public void DbCreated_AfterInitialization()
  {
    bool fileExists = File.Exists($"{BasePath}/{ApplicationName}/Data.db");
    Assert.That(fileExists, Is.True);
  }

  [Test]
  [Description("Tests that an object can be updated")]
  public async Task UpdateObject_AfterAdd()
  {
    const string payloadId = "MyTestObjectId";
    const string payloadData = "MyTestObjectData";

    _sqlite.SaveObject(payloadId, payloadData);
    await _sqlite.WriteComplete().ConfigureAwait(false);

    const string newPayload = "MyEvenBetterObjectData";
    _sqlite.UpdateObject(payloadId, newPayload);
    await _sqlite.WriteComplete().ConfigureAwait(false);

    var result = _sqlite.GetObject(payloadId);
    Assert.That(result, Is.EqualTo(newPayload));
  }

  [Test]
  [Description("Tests that updating an object that hasn't been saved previously adds the object to the DB")]
  public async Task UpdateObject_WhenMissing()
  {
    const string payloadId = "MyTestObjectId";
    const string payloadData = "MyTestObjectData";

    var preUpdate = _sqlite.GetObject(payloadId);
    Assert.That(preUpdate, Is.Null);

    _sqlite.UpdateObject(payloadId, payloadData);
    await _sqlite.WriteComplete().ConfigureAwait(false);

    var postUpdate = _sqlite.GetObject(payloadId);
    Assert.That(postUpdate, Is.EqualTo(payloadData));
  }

  [Test]
  public async Task SaveAndRetrieveObject_Sync()
  {
    const string payloadId = "MyTestObjectId";
    const string payloadData = "MyTestObjectData";

    {
      var preAdd = Sut.GetObject(payloadId);
      Assert.That(preAdd, Is.Null);
    }

    _sqlite.SaveObjectSync(payloadId, payloadData);

    {
      var postAdd = Sut.GetObject(payloadId);
      Assert.That(postAdd, Is.EqualTo(payloadData));
    }
  }

  public void Dispose()
  {
    _sqlite?.Dispose();
  }
}
