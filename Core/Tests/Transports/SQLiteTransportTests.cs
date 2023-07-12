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

  public void Dispose()
  {
    _sqlite?.Dispose();
  }
}
