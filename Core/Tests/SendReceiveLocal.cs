using System.Collections.Concurrent;
using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace TestsUnit;

[TestFixture]
public class SendReceiveLocal
{
  private string objId_01,
    commitId_02;

  private int numObjects = 3001;

  [Test(Description = "Pushing a commit locally"), Order(1)]
  public void LocalUpload()
  {
    var myObject = new Base();
    var rand = new Random();

    myObject["@items"] = new List<Base>();

    for (int i = 0; i < numObjects; i++)
      ((List<Base>)myObject["@items"]).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-___/---" });

    objId_01 = Operations.Send(myObject).Result;

    Assert.NotNull(objId_01);
    TestContext.Out.WriteLine($"Written {numObjects + 1} objects. Commit id is {objId_01}");
  }

  [Test(Description = "Pulling a commit locally"), Order(2)]
  public void LocalDownload()
  {
    var commitPulled = Operations.Receive(objId_01).Result;

    Assert.That(typeof(Point), Is.EqualTo(((List<object>)commitPulled["@items"])[0].GetType()));
    Assert.That(numObjects, Is.EqualTo(((List<object>)commitPulled["@items"]).Count));
  }

  [Test(Description = "Pushing and Pulling a commit locally")]
  public void LocalUploadDownload()
  {
    var myObject = new Base();
    myObject["@items"] = new List<Base>();

    var rand = new Random();

    for (int i = 0; i < numObjects; i++)
      ((List<Base>)myObject["@items"]).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-___/---" });

    objId_01 = Operations.Send(myObject).Result;

    var commitPulled = Operations.Receive(objId_01).Result;

    Assert.That(typeof(Point), Is.EqualTo(((List<object>)commitPulled["@items"])[0].GetType()));
    Assert.That(numObjects, Is.EqualTo(((List<object>)commitPulled["@items"]).Count));
  }

  [Test(Description = "Pushing and pulling a commit locally"), Order(3)]
  public async Task LocalUploadDownloadSmall()
  {
    var myObject = new Base();
    myObject["@items"] = new List<Base>();

    var rand = new Random();

    for (int i = 0; i < 30; i++)
      ((List<Base>)myObject["@items"]).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-ugh/---" });

    objId_01 = await Operations.Send(myObject).ConfigureAwait(false);

    Assert.NotNull(objId_01);
    TestContext.Out.WriteLine($"Written {numObjects + 1} objects. Commit id is {objId_01}");

    var objsPulled = await Operations.Receive(objId_01).ConfigureAwait(false);
    Assert.That(((List<object>)objsPulled["@items"]).Count, Is.EqualTo(30));
  }

  [Test(Description = "Pushing and pulling a commit locally"), Order(3)]
  public async Task LocalUploadDownloadListDic()
  {
    var myList = new List<object> { 1, 2, 3, "ciao" };
    var myDic = new Dictionary<string, object> { { "a", myList }, { "b", 2 }, { "c", "ciao" } };

    var myObject = new Base();
    myObject["@dictionary"] = myDic;
    myObject["@list"] = myList;

    objId_01 = await Operations.Send(myObject).ConfigureAwait(false);

    Assert.NotNull(objId_01);

    var objsPulled = await Operations.Receive(objId_01).ConfigureAwait(false);
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
      ((List<Base>)((dynamic)obj).LayerA).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "foo" });

    for (int i = 0; i < 30; i++)
      ((List<Base>)((dynamic)obj).LayerB).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "bar" });

    for (int i = 0; i < 30; i++)
      ((List<Base>)((dynamic)obj)["@LayerC"]).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "baz" });

    objId_01 = await Operations.Send(obj).ConfigureAwait(false);

    Assert.NotNull(objId_01);
    TestContext.Out.WriteLine($"Written {numObjects + 1} objects. Commit id is {objId_01}");

    var objPulled = await Operations.Receive(objId_01).ConfigureAwait(false);

    Assert.That(typeof(Base), Is.EqualTo(objPulled.GetType()));

    // Note: even if the layers were originally declared as lists of "Base" objects, on deserialisation we cannot know that,
    // as it's a dynamic property. Dynamic properties, if their content value is ambigous, will default to a common-sense standard.
    // This specifically manifests in the case of lists and dictionaries: List<AnySpecificType> will become List<object>, and
    // Dictionary<string, MyType> will deserialize to Dictionary<string,object>.
    var layerA = ((dynamic)objPulled)["LayerA"] as List<object>;
    Assert.That(layerA.Count, Is.EqualTo(30));

    var layerC = ((dynamic)objPulled)["@LayerC"] as List<object>;
    Assert.That(layerC.Count, Is.EqualTo(30));
    Assert.That(typeof(Point), Is.EqualTo(layerC[0].GetType()));

    var layerD = ((dynamic)objPulled)["@LayerD"] as List<object>;
    Assert.That(layerD.Count, Is.EqualTo(2));
  }

  [Test(Description = "Should show progress!"), Order(4)]
  public async Task UploadProgressReports()
  {
    var myObject = new Base();
    myObject["items"] = new List<Base>();
    var rand = new Random();

    for (int i = 0; i < 30; i++)
      ((List<Base>)myObject["items"]).Add(new Point(i, i, i + rand.NextDouble()) { applicationId = i + "-fab/---" });

    ConcurrentDictionary<string, int> progress = null;
    commitId_02 = await Operations
      .Send(
        myObject,
        onProgressAction: dict =>
        {
          progress = dict;
        }
      )
      .ConfigureAwait(false);

    Assert.NotNull(progress);
    Assert.GreaterOrEqual(progress.Keys.Count, 1);
  }

  [Test(Description = "Should show progress!"), Order(5)]
  public async Task DownloadProgressReports()
  {
    ConcurrentDictionary<string, int> progress = null;
    var pulledCommit = await Operations
      .Receive(
        commitId_02,
        onProgressAction: dict =>
        {
          progress = dict;
        }
      )
      .ConfigureAwait(false);
    Assert.NotNull(progress);
    Assert.GreaterOrEqual(progress.Keys.Count, 1);
  }

  [Test(Description = "Should dispose of transports after a send or receive operation if so specified.")]
  public async Task ShouldDisposeTransports()
  {
    var @base = new Base();
    @base["test"] = "the best";

    var myLocalTransport = new SQLiteTransport();
    var id = await Operations
      .Send(@base, new List<ITransport> { myLocalTransport }, false, disposeTransports: true)
      .ConfigureAwait(false);

    // Send
    try
    {
      await Operations
        .Send(@base, new List<ITransport> { myLocalTransport }, false, disposeTransports: true)
        .ConfigureAwait(false);
      Assert.Fail("Send operation did not dispose of transport.");
    }
    catch (Exception)
    {
      // Pass
    }

    myLocalTransport = myLocalTransport.Clone() as SQLiteTransport;
    var obj = await Operations.Receive(id, null, myLocalTransport, disposeTransports: true).ConfigureAwait(false);

    try
    {
      await Operations.Receive(id, null, myLocalTransport).ConfigureAwait(false);
      Assert.Fail("Receive operation did not dispose of transport.");
    }
    catch
    {
      // Pass
    }
  }

  [Test(Description = "Should not dispose of transports if so specified.")]
  public async Task ShouldNotDisposeTransports()
  {
    var @base = new Base();
    @base["test"] = "the best";

    var myLocalTransport = new SQLiteTransport();
    var id = await Operations.Send(@base, new List<ITransport> { myLocalTransport }, false).ConfigureAwait(false);
    await Operations.Send(@base, new List<ITransport> { myLocalTransport }, false).ConfigureAwait(false);

    var obj = await Operations.Receive(id, null, myLocalTransport).ConfigureAwait(false);
    await Operations.Receive(id, null, myLocalTransport).ConfigureAwait(false);
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
}
