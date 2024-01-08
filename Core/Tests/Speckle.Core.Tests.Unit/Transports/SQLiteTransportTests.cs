using Microsoft.Data.Sqlite;
using NUnit.Framework;
using Speckle.Core.Transports;

namespace Speckle.Core.Tests.Unit.Transports;

[TestFixture]
[TestOf(nameof(SQLiteTransport))]
public sealed class SQLiteTransportTests : TransportTests, IDisposable
{
  protected override ITransport Sut => _sqlite!;

  private SQLiteTransport _sqlite;

  private static readonly string s_basePath = $"./temp {Guid.NewGuid()}";
  private const string APPLICATION_NAME = "Speckle Integration Tests";

  [SetUp]
  public void Setup()
  {
    _sqlite = new SQLiteTransport(s_basePath, APPLICATION_NAME);
  }

  [TearDown]
  public void TearDown()
  {
    this.Dispose();
    SqliteConnection.ClearAllPools();
    Directory.Delete(s_basePath, true);
    _sqlite = null;
  }

  [Test]
  public void DbCreated_AfterInitialization()
  {
    bool fileExists = File.Exists($"{s_basePath}/{APPLICATION_NAME}/Data.db");
    Assert.That(fileExists, Is.True);
  }

  [Test]
  [Description("Tests that an object can be updated")]
  public async Task UpdateObject_AfterAdd()
  {
    const string PAYLOAD_ID = "MyTestObjectId";
    const string PAYLOAD_DATA = "MyTestObjectData";

    _sqlite.SaveObject(PAYLOAD_ID, PAYLOAD_DATA);
    await _sqlite.WriteComplete().ConfigureAwait(false);

    const string NEW_PAYLOAD = "MyEvenBetterObjectData";
    _sqlite.UpdateObject(PAYLOAD_ID, NEW_PAYLOAD);
    await _sqlite.WriteComplete().ConfigureAwait(false);

    var result = _sqlite.GetObject(PAYLOAD_ID);
    Assert.That(result, Is.EqualTo(NEW_PAYLOAD));
  }

  [Test]
  [Description("Tests that updating an object that hasn't been saved previously adds the object to the DB")]
  public async Task UpdateObject_WhenMissing()
  {
    const string PAYLOAD_ID = "MyTestObjectId";
    const string PAYLOAD_DATA = "MyTestObjectData";

    var preUpdate = _sqlite.GetObject(PAYLOAD_ID);
    Assert.That(preUpdate, Is.Null);

    _sqlite.UpdateObject(PAYLOAD_ID, PAYLOAD_DATA);
    await _sqlite.WriteComplete().ConfigureAwait(false);

    var postUpdate = _sqlite.GetObject(PAYLOAD_ID);
    Assert.That(postUpdate, Is.EqualTo(PAYLOAD_DATA));
  }

  [Test]
  public void SaveAndRetrieveObject_Sync()
  {
    const string PAYLOAD_ID = "MyTestObjectId";
    const string PAYLOAD_DATA = "MyTestObjectData";

    {
      var preAdd = Sut.GetObject(PAYLOAD_ID);
      Assert.That(preAdd, Is.Null);
    }

    _sqlite.SaveObjectSync(PAYLOAD_ID, PAYLOAD_DATA);

    {
      var postAdd = Sut.GetObject(PAYLOAD_ID);
      Assert.That(postAdd, Is.EqualTo(PAYLOAD_DATA));
    }
  }

  public void Dispose()
  {
    _sqlite?.Dispose();
  }
}
