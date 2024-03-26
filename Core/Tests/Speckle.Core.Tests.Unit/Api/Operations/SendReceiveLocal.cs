using System.Collections.Concurrent;
using NUnit.Framework;
using Speckle.Core.Models;
using Speckle.Core.Tests.Unit.Kits;
using Speckle.Core.Transports;

namespace Speckle.Core.Tests.Unit.Api.Operations;

[TestFixture]
public sealed class SendReceiveLocal : IDisposable
{
  private string _objId01,
    _commitId02;

  private const int NUM_OBJECTS = 3001;

  private readonly SQLiteTransport _sut = new();

  [Test(Description = "Pushing a commit locally"), Order(1)]
  public void LocalUpload()
  {
    var myObject = new Base();
    var rand = new Random();

    myObject["@items"] = new List<Base>();

    for (int i = 0; i < NUM_OBJECTS; i++)
    {
      ((List<Base>)myObject["@items"]).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-___/---" });
    }

    using SQLiteTransport localTransport = new();
    _objId01 = Core.Api.Operations.Send(myObject, localTransport, false).Result;

    Assert.That(_objId01, Is.Not.Null);
    TestContext.Out.WriteLine($"Written {NUM_OBJECTS + 1} objects. Commit id is {_objId01}");
  }

  [Test(Description = "Pulling a commit locally"), Order(2)]
  public void LocalDownload()
  {
    var commitPulled = Core.Api.Operations.Receive(_objId01).Result;

    Assert.That(((List<object>)commitPulled["@items"])[0], Is.TypeOf<Point>());
    Assert.That(((List<object>)commitPulled["@items"]), Has.Count.EqualTo(NUM_OBJECTS));
  }

  [Test(Description = "Pushing and Pulling a commit locally")]
  public void LocalUploadDownload()
  {
    var myObject = new Base();
    myObject["@items"] = new List<Base>();

    var rand = new Random();

    for (int i = 0; i < NUM_OBJECTS; i++)
    {
      ((List<Base>)myObject["@items"]).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-___/---" });
    }

    _objId01 = Core.Api.Operations.Send(myObject, _sut, false).Result;

    var commitPulled = Core.Api.Operations.Receive(_objId01).Result;
    List<object> items = (List<object>)commitPulled["@items"];

    Assert.That(items, Has.All.TypeOf<Point>());
    Assert.That(items, Has.Count.EqualTo(NUM_OBJECTS));
  }

  [Test(Description = "Pushing and pulling a commit locally"), Order(3)]
  public async Task LocalUploadDownloadSmall()
  {
    var myObject = new Base();
    myObject["@items"] = new List<Base>();

    var rand = new Random();

    for (int i = 0; i < 30; i++)
    {
      ((List<Base>)myObject["@items"]).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-ugh/---" });
    }

    _objId01 = await Core.Api.Operations.Send(myObject, _sut, false);

    Assert.That(_objId01, Is.Not.Null);
    TestContext.Out.WriteLine($"Written {NUM_OBJECTS + 1} objects. Commit id is {_objId01}");

    var objsPulled = await Core.Api.Operations.Receive(_objId01);
    Assert.That(((List<object>)objsPulled["@items"]), Has.Count.EqualTo(30));
  }

  [Test(Description = "Pushing and pulling a commit locally"), Order(3)]
  public async Task LocalUploadDownloadListDic()
  {
    var myList = new List<object> { 1, 2, 3, "ciao" };
    var myDic = new Dictionary<string, object>
    {
      { "a", myList },
      { "b", 2 },
      { "c", "ciao" }
    };

    var myObject = new Base();
    myObject["@dictionary"] = myDic;
    myObject["@list"] = myList;

    _objId01 = await Core.Api.Operations.Send(myObject, _sut, false);

    Assert.That(_objId01, Is.Not.Null);

    var objsPulled = await Core.Api.Operations.Receive(_objId01);
    Assert.That(((List<object>)((Dictionary<string, object>)objsPulled["@dictionary"])["a"]).First(), Is.EqualTo(1));
    Assert.That(((List<object>)objsPulled["@list"]).Last(), Is.EqualTo("ciao"));
  }

  [Test(Description = "Pushing and pulling a random object, with our without detachment"), Order(3)]
  public async Task UploadDownloadNonCommitObject()
  {
    var obj = new Base();
    // Here we are creating a "non-standard" object to act as a base for our multiple objects.
    ((dynamic)obj).LayerA = new List<Base>(); // Layer a and b will be stored "in" the parent object,
    ((dynamic)obj).LayerB = new List<Base>();
    ((dynamic)obj)["@LayerC"] = new List<Base>(); // whereas this "layer" will be stored as references only.
    ((dynamic)obj)["@LayerD"] = new Point[] { new(), new(12, 3, 4) };
    var rand = new Random();

    for (int i = 0; i < 30; i++)
    {
      ((List<Base>)((dynamic)obj).LayerA).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "foo" });
    }

    for (int i = 0; i < 30; i++)
    {
      ((List<Base>)((dynamic)obj).LayerB).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "bar" });
    }

    for (int i = 0; i < 30; i++)
    {
      ((List<Base>)((dynamic)obj)["@LayerC"]).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "baz" });
    }

    _objId01 = await Core.Api.Operations.Send(obj, _sut, false);

    Assert.That(_objId01, Is.Not.Null);
    TestContext.Out.WriteLine($"Written {NUM_OBJECTS + 1} objects. Commit id is {_objId01}");

    var objPulled = await Core.Api.Operations.Receive(_objId01);

    Assert.That(objPulled, Is.TypeOf<Base>());

    // Note: even if the layers were originally declared as lists of "Base" objects, on deserialisation we cannot know that,
    // as it's a dynamic property. Dynamic properties, if their content value is ambigous, will default to a common-sense standard.
    // This specifically manifests in the case of lists and dictionaries: List<AnySpecificType> will become List<object>, and
    // Dictionary<string, MyType> will deserialize to Dictionary<string,object>.
    var layerA = ((dynamic)objPulled)["LayerA"] as List<object>;
    Assert.That(layerA, Has.Count.EqualTo(30));

    var layerC = ((dynamic)objPulled)["@LayerC"] as List<object>;
    Assert.That(layerC, Has.Count.EqualTo(30));
    Assert.That(layerC[0], Is.TypeOf<Point>());

    var layerD = ((dynamic)objPulled)["@LayerD"] as List<object>;
    Assert.That(layerD, Has.Count.EqualTo(2));
  }

  [Test(Description = "Should show progress!"), Order(4)]
  public async Task UploadProgressReports()
  {
    Base myObject = new() { ["items"] = new List<Base>() };
    var rand = new Random();

    for (int i = 0; i < 30; i++)
    {
      ((List<Base>)myObject["items"]).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-fab/---" });
    }

    ConcurrentDictionary<string, int> progress = null;
    _commitId02 = await Core.Api.Operations.Send(
      myObject,
      _sut,
      false,
      onProgressAction: dict =>
      {
        progress = dict;
      }
    );

    Assert.That(progress, Is.Not.Null);
    Assert.That(progress!.Keys, Has.Count.GreaterThanOrEqualTo(1));
  }

  [Test(Description = "Should show progress!"), Order(5)]
  public async Task DownloadProgressReports()
  {
    ConcurrentDictionary<string, int> progress = null;
    var pulledCommit = await Core.Api.Operations.Receive(
      _commitId02,
      onProgressAction: dict =>
      {
        progress = dict;
      }
    );
    Assert.That(progress, Is.Not.Null);
    Assert.That(progress.Keys, Has.Count.GreaterThanOrEqualTo(1));
  }

  [Test(Description = "Should dispose of transports after a send or receive operation if so specified.")]
  [Obsolete("Send overloads that perform disposal are deprecated")]
  public async Task ShouldDisposeTransports()
  {
    var @base = new Base();
    @base["test"] = "the best";

    var myLocalTransport = new SQLiteTransport();
    var id = await Core.Api.Operations.Send(
      @base,
      new List<ITransport> { myLocalTransport },
      false,
      disposeTransports: true
    );

    // Send
    Assert.ThrowsAsync<ObjectDisposedException>(
      async () =>
        await Core.Api.Operations.Send(@base, new List<ITransport> { myLocalTransport }, false, disposeTransports: true)
    );

    myLocalTransport = myLocalTransport.Clone() as SQLiteTransport;
    _ = await Core.Api.Operations.Receive(id, null, myLocalTransport, disposeTransports: true);

    Assert.ThrowsAsync<InvalidOperationException>(
      async () => await Core.Api.Operations.Receive(id, null, myLocalTransport)
    );
  }

  [Test(Description = "Should not dispose of transports if so specified.")]
  public async Task ShouldNotDisposeTransports()
  {
    var @base = new Base();
    @base["test"] = "the best";

    SQLiteTransport myLocalTransport = new();
    var id = await Core.Api.Operations.Send(@base, myLocalTransport, false);
    await Core.Api.Operations.Send(@base, myLocalTransport, false);

    _ = await Core.Api.Operations.Receive(id, null, myLocalTransport);
    await Core.Api.Operations.Receive(id, null, myLocalTransport);
  }

  //[Test]
  //public async Task DiskTransportTest()
  //{
  //  var myObject = new Base();
  //  myObject["@items"] = new List<Base>();
  //  myObject["test"] = "random";

  //  var rand = new Random();

  //  for (int i = 0; i < 100; i++)
  //  {
  //    ((List<Base>)myObject["@items"]).Add(new Point(i, i, i) { applicationId = i + "-___/---" });
  //  }

  //  var dt = new DiskTransport.DiskTransport();
  //  var id = await Operations.Send(myObject, new List<ITransport>() { dt }, false);

  //  Assert.IsNotNull(id);

  //  var rebase = await Operations.Receive(id, dt);

  //  Assert.AreEqual(rebase.GetId(true), id);
  //}

  public void Dispose()
  {
    _sut.Dispose();
  }
}
