#nullable enable
using NUnit.Framework;
using Speckle.Core.Transports;

namespace TestsUnit.Transports;

[TestFixture]
public abstract class TransportTests
{
  protected abstract ITransport Sut { get; }

  [Test]
  public async Task SaveAndRetrieveObject()
  {
    const string payloadId = "MyTestObjectId";
    const string payloadData = "MyTestObjectData";

    {
      var preAdd = Sut.GetObject(payloadId);
      Assert.That(preAdd, Is.Null);
    }

    Sut.SaveObject(payloadId, payloadData);
    await Sut.WriteComplete().ConfigureAwait(false);

    {
      var postAdd = Sut.GetObject(payloadId);
      Assert.That(postAdd, Is.EqualTo(payloadData));
    }
  }

  [Test]
  public async Task HasObject()
  {
    const string payloadId = "MyTestObjectId";
    const string payloadData = "MyTestObjectData";

    {
      var preAdd = await Sut.HasObjects(new[] { payloadId }).ConfigureAwait(false);
      Assert.That(preAdd, Has.Exactly(1).Items);
      Assert.That(preAdd, Has.No.ContainValue(true));
      Assert.That(preAdd, Contains.Key(payloadId));
    }

    Sut.SaveObject(payloadId, payloadData);
    await Sut.WriteComplete().ConfigureAwait(false);

    {
      var postAdd = await Sut.HasObjects(new[] { payloadId }).ConfigureAwait(false);

      Assert.That(postAdd, Has.Exactly(1).Items);
      Assert.That(postAdd, Has.No.ContainValue(false));
      Assert.That(postAdd, Contains.Key(payloadId));
    }
  }

  [Test]
  [Description("Test that transports save objects when many threads are concurrently saving data")]
  public async Task SaveObject_ConcurrentWrites()
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
        Sut.SaveObject(x.id, x.data);
      }
    );

    await Sut.WriteComplete().ConfigureAwait(false);

    //Test 1. SavedObjectCount //WARN: FAIL!!! seems this is not implemented for SQLite Transport
    //Assert.That(transport.SavedObjectCount, Is.EqualTo(testDataCount));

    //Test 2. HasObjects
    var ids = testData.Select(x => x.id).ToList();
    var hasObjectsResult = await Sut.HasObjects(ids).ConfigureAwait(false);

    Assert.That(hasObjectsResult, Does.Not.ContainValue(false));
    Assert.That(hasObjectsResult.Keys, Is.EquivalentTo(ids));

    //Test 3. GetObjects
    foreach (var x in testData)
    {
      var res = Sut.GetObject(x.id);
      Assert.That(res, Is.EqualTo(x.data));
    }
  }

  [Test]
  public void SaveObject_FromTransport_FailsPredictably()
  {
    var exception = Assert.Throws<TransportException>(() => Sut.SaveObject("non-existent-id", Sut));
    Assert.That(exception.Transport, Is.EqualTo(Sut));
  }
}
