#nullable enable
using NUnit.Framework;
using Speckle.Core.Transports;

namespace TestsUnit;

public class TransportTests
{
  [Test]
  [TestCaseSource(nameof(SyncTransports))]
  public void SaveObject_FailsPredictably_WhenIdNotFound(ITransport transport)
  {
    Assert.Throws<TransportException>(() => transport.SaveObject("non-existent-id", transport));
  }

  [TestCaseSource(nameof(SyncTransports))]
  public async Task GetObject_ReturnsObject_AfterObjectIsAdded(ITransport transport)
  {
    const string payload = "Payload data 123123";
    const string payloadId = "myId";

    transport.SaveObject(payloadId, payload);
    await transport.WriteComplete().ConfigureAwait(false);

    var result = transport.GetObject(payloadId);

    Assert.That(result, Is.EqualTo(payload));
  }

  [TestCaseSource(nameof(ConcurrentCapableTransports))]
  public async Task SaveObject_SavesObjects_WithConcurrentWrites(ITransport transport)
  {
    const int testDataCount = 100;
    List<(string id, string data)> testData = Enumerable
      .Range(0, testDataCount)
      .Select(_ => (Guid.NewGuid().ToString(), Guid.NewGuid().ToString()))
      .ToList();

    Parallel.ForEach(
      testData,
      x =>
      {
        transport.SaveObject(x.id, x.data);
      }
    );

    await transport.WriteComplete().ConfigureAwait(false);

    //Test 1. SavedObjectCount
    //Assert.That(transport.SavedObjectCount, Is.EqualTo(testDataCount));

    //Test 2. HasObjects
    var ids = testData.Select(x => x.id).ToList();
    var hasObjectsResult = await transport.HasObjects(ids).ConfigureAwait(false);

    Assert.That(hasObjectsResult, Does.Not.ContainValue(false));
    Assert.That(hasObjectsResult.Keys, Is.EquivalentTo(ids));

    //Test 3. GetObjects
    foreach (var x in testData)
    {
      var res = transport.GetObject(x.id);
      Assert.That(res, Is.EqualTo(x.data));
    }
  }

  public static IEnumerable<ITransport> SyncTransports =>
    new ITransport[] { new MemoryTransport(), new SQLiteTransport() };

  public static IEnumerable<ITransport> ConcurrentCapableTransports => new ITransport[] { new SQLiteTransport() };
}
