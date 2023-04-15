using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace TestsIntegration;

public class ServerTransportTests
{
  private string _basePath;
  public Account account;
  public Client client;
  public string streamId;
  public ServerTransport transport;

  [OneTimeSetUp]
  public async Task InitialSetup()
  {
    _basePath = Path.Join(Path.GetTempPath(), "speckleTest");

    CleanData();
    Directory.CreateDirectory(_basePath);
    SpecklePathProvider.OverrideApplicationDataPath(_basePath);

    account = await Fixtures.SeedUser().ConfigureAwait(false);
    client = new Client(account);
    streamId = client.StreamCreate(new StreamCreateInput { description = "Flobber", name = "Blobber" }).Result;
  }

  [SetUp]
  public void Setup()
  {
    CleanData();
    // need to recreate the server transport object for each test
    // to make sure all folders are properly initialized
    transport = new ServerTransport(account, streamId);
  }

  [TearDown]
  public void TearDown()
  {
    CleanData();
  }

  private void CleanData()
  {
    try
    {
      Directory.Delete(_basePath, true);
    }
    catch (DirectoryNotFoundException) { }
  }

  [Test]
  public async Task SendObject()
  {
    var myObject = Fixtures.GenerateNestedObject();

    var objectId = await Operations.Send(myObject, new List<ITransport> { transport }).ConfigureAwait(false);

    var test = objectId;
    Assert.IsNotNull(test);
  }

  [Test]
  public async Task SendAndReceiveObjectWithBlobs()
  {
    var myObject = Fixtures.GenerateSimpleObject();
    myObject["blobs"] = Fixtures.GenerateThreeBlobs();

    var sentObjectId = await Operations.Send(myObject, new List<ITransport> { transport }).ConfigureAwait(false);

    // NOTE: used to debug diffing
    // await Operations.Send(myObject, new List<ITransport> { transport });

    var receivedObject = await Operations.Receive(sentObjectId, transport).ConfigureAwait(false);

    var allFiles = Directory
      .GetFiles(transport.BlobStorageFolder)
      .Select(fp => fp.Split(Path.DirectorySeparatorChar).Last())
      .ToList();
    var blobPaths = allFiles
      .Where(fp => fp.Length > Blob.LocalHashPrefixLength) // excludes things like .DS_store
      .ToList();

    // Check that there are three downloaded blobs!
    Assert.That(blobPaths.Count, Is.EqualTo(3));

    var blobs = (receivedObject["blobs"] as List<object>).Cast<Blob>().ToList();
    // Check that we have three blobs
    Assert.IsTrue(blobs.Count == 3);
    // Check that received blobs point to local path (where they were received)
    Assert.IsTrue(blobs[0].filePath.Contains(transport.BlobStorageFolder));
    Assert.IsTrue(blobs[1].filePath.Contains(transport.BlobStorageFolder));
    Assert.IsTrue(blobs[2].filePath.Contains(transport.BlobStorageFolder));
  }

  [Test]
  public async Task SendWithBlobsWithoutSQLiteSendCache()
  {
    var myObject = Fixtures.GenerateSimpleObject();
    myObject["blobs"] = Fixtures.GenerateThreeBlobs();

    var memTransport = new MemoryTransport();
    var sentObjectId = await Operations
      .Send(myObject, new List<ITransport> { transport, memTransport }, false)
      .ConfigureAwait(false);

    var receivedObject = await Operations.Receive(sentObjectId, transport).ConfigureAwait(false);

    var allFiles = Directory
      .GetFiles(transport.BlobStorageFolder)
      .Select(fp => fp.Split(Path.DirectorySeparatorChar).Last())
      .ToList();
    var blobPaths = allFiles
      .Where(fp => fp.Length > Blob.LocalHashPrefixLength) // excludes things like .DS_store
      .ToList();

    // Check that there are three downloaded blobs!
    Assert.That(blobPaths.Count, Is.EqualTo(3));

    var blobs = (receivedObject["blobs"] as List<object>).Cast<Blob>().ToList();
    // Check that we have three blobs
    Assert.IsTrue(blobs.Count == 3);
    // Check that received blobs point to local path (where they were received)
    Assert.IsTrue(blobs[0].filePath.Contains(transport.BlobStorageFolder));
    Assert.IsTrue(blobs[1].filePath.Contains(transport.BlobStorageFolder));
    Assert.IsTrue(blobs[2].filePath.Contains(transport.BlobStorageFolder));
  }

  [Test]
  public async Task SendReceiveWithCleanedMemoryCache()
  {
    var myObject = Fixtures.GenerateSimpleObject();
    myObject["blobs"] = Fixtures.GenerateThreeBlobs();

    var memTransport = new MemoryTransport();
    var sentObjectId = await Operations
      .Send(myObject, new List<ITransport> { transport, memTransport }, false)
      .ConfigureAwait(false);

    memTransport = new MemoryTransport();
    var receivedObject = await Operations
      .Receive(
        sentObjectId,
        transport,
        memTransport,
        onErrorAction: (s, e) =>
        {
          Console.WriteLine(s);
        }
      )
      .ConfigureAwait(false);

    var allFiles = Directory
      .GetFiles(transport.BlobStorageFolder)
      .Select(fp => fp.Split(Path.DirectorySeparatorChar).Last())
      .ToList();
    var blobPaths = allFiles
      .Where(fp => fp.Length > Blob.LocalHashPrefixLength) // excludes things like .DS_store
      .ToList();

    // Check that there are three downloaded blobs!
    Assert.That(blobPaths.Count, Is.EqualTo(3));

    var blobs = (receivedObject["blobs"] as List<object>).Cast<Blob>().ToList();
    // Check that we have three blobs
    Assert.IsTrue(blobs.Count == 3);
    // Check that received blobs point to local path (where they were received)
    Assert.IsTrue(blobs[0].filePath.Contains(transport.BlobStorageFolder));
    Assert.IsTrue(blobs[1].filePath.Contains(transport.BlobStorageFolder));
    Assert.IsTrue(blobs[2].filePath.Contains(transport.BlobStorageFolder));
  }
}
