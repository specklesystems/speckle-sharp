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
    _sqlite?.Dispose();
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
    await _sqlite.WriteComplete();

    const string NEW_PAYLOAD = "MyEvenBetterObjectData";
    _sqlite.UpdateObject(PAYLOAD_ID, NEW_PAYLOAD);
    await _sqlite.WriteComplete();

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
    await _sqlite.WriteComplete();

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

  [Test(
    Description = "Tests that it is possible to enumerate through all objects of the transport while updating them, without getting stuck in an infinite loop"
  )]
  [Timeout(1000)]
  public void UpdateObject_WhileEnumerating()
  {
    //I question if this is the behaviour we want, but AccountManager.GetObjects is relying on being able to update objects while enumerating over them
    const string UPDATE_STRING = "_new";
    Dictionary<string, string> testData =
      new()
      {
        { "a", "This is object a" },
        { "b", "This is object b" },
        { "c", "This is object c" },
        { "d", "This is object d" }
      };
    int length = testData.Values.First().Length;

    foreach (var (key, data) in testData)
    {
      _sqlite.SaveObjectSync(key, data);
    }

    foreach (var o in _sqlite.GetAllObjects())
    {
      string newData = o + UPDATE_STRING;
      string key = $"{o[length - 1]}";

      _sqlite.UpdateObject(key, newData);
    }

    //Assert that objects were updated
    Assert.That(_sqlite.GetAllObjects().ToList(), Has.All.Contains(UPDATE_STRING));
    //Assert that objects were only updated once
    Assert.That(_sqlite.GetAllObjects().ToList(), Has.All.Length.EqualTo(length + UPDATE_STRING.Length));
  }

  public void Dispose()
  {
    _sqlite?.Dispose();
  }
}
