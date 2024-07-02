using System.Collections;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Helpers;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace Speckle.Core.Tests.Integration;

public class ServerTransportTests : IDisposable
{
  private string _basePath;
  private Account _account;
  private Client _client;
  private string _streamId;
  private ServerTransport _transport;

  [OneTimeSetUp]
  public async Task InitialSetup()
  {
    _basePath = Path.Join(Path.GetTempPath(), "speckleTest");

    CleanData();
    Directory.CreateDirectory(_basePath);
    SpecklePathProvider.OverrideApplicationDataPath(_basePath);

    _account = await Fixtures.SeedUser();
    _client = new Client(_account);
    _streamId = await _client.StreamCreate(new StreamCreateInput { description = "Flobber", name = "Blobber" });
  }

  [SetUp]
  public void Setup()
  {
    CleanData();
    // need to recreate the server transport object for each test
    // to make sure all folders are properly initialized
    _transport = new ServerTransport(_account, _streamId);
  }

  [TearDown]
  public void TearDown()
  {
    CleanData();
  }

  private void CleanData()
  {
    _transport?.Dispose();
    if (Directory.Exists(_basePath))
    {
      Directory.Delete(_basePath, true);
    }
  }

  [Test]
  public async Task SendObject()
  {
    var myObject = Fixtures.GenerateNestedObject();

    var objectId = await Operations.Send(myObject, _transport, false);

    Assert.That(objectId, Is.Not.Null);
  }

  [Test]
  public async Task SendAndReceiveObjectWithBlobs()
  {
    var myObject = Fixtures.GenerateSimpleObject();
    myObject["blobs"] = Fixtures.GenerateThreeBlobs();

    var sentObjectId = await Operations.Send(myObject, _transport, false);

    // NOTE: used to debug diffing
    // await Operations.Send(myObject, new List<ITransport> { transport });

    var receivedObject = await Operations.Receive(sentObjectId, _transport, new MemoryTransport());

    var allFiles = Directory
      .GetFiles(_transport.BlobStorageFolder)
      .Select(fp => fp.Split(Path.DirectorySeparatorChar).Last())
      .ToList();
    var blobPaths = allFiles
      .Where(fp => fp.Length > Blob.LocalHashPrefixLength) // excludes things like .DS_store
      .ToList();

    // Check that there are three downloaded blobs!
    Assert.That(blobPaths, Has.Count.EqualTo(3));

    var blobs = ((IList<object>)receivedObject["blobs"]!).Cast<Blob>().ToList();
    // Check that we have three blobs
    Assert.That(blobs, Has.Count.EqualTo(3));
    // Check that received blobs point to local path (where they were received)
    Assert.That(blobs[0].filePath, Contains.Substring(_transport.BlobStorageFolder));
    Assert.That(blobs[1].filePath, Contains.Substring(_transport.BlobStorageFolder));
    Assert.That(blobs[2].filePath, Contains.Substring(_transport.BlobStorageFolder));
  }

  [Test]
  public async Task SendWithBlobsWithoutSQLiteSendCache()
  {
    var myObject = Fixtures.GenerateSimpleObject();
    myObject["blobs"] = Fixtures.GenerateThreeBlobs();

    var memTransport = new MemoryTransport();
    var sentObjectId = await Operations.Send(myObject, new List<ITransport> { _transport, memTransport });

    var receivedObject = await Operations.Receive(sentObjectId, _transport, new MemoryTransport());

    var allFiles = Directory
      .GetFiles(_transport.BlobStorageFolder)
      .Select(fp => fp.Split(Path.DirectorySeparatorChar).Last())
      .ToList();
    var blobPaths = allFiles
      .Where(fp => fp.Length > Blob.LocalHashPrefixLength) // excludes things like .DS_store
      .ToList();

    // Check that there are three downloaded blobs!
    Assert.That(blobPaths, Has.Count.EqualTo(3));

    var blobs = ((IList<object>)receivedObject["blobs"]!).Cast<Blob>().ToList();
    // Check that we have three blobs
    Assert.That(blobs, Has.Count.EqualTo(3));
    // Check that received blobs point to local path (where they were received)
    Assert.That(blobs[0].filePath, Contains.Substring(_transport.BlobStorageFolder));
    Assert.That(blobs[1].filePath, Contains.Substring(_transport.BlobStorageFolder));
    Assert.That(blobs[2].filePath, Contains.Substring(_transport.BlobStorageFolder));
  }

  [Test]
  public async Task SendReceiveWithCleanedMemoryCache()
  {
    var myObject = Fixtures.GenerateSimpleObject();
    myObject["blobs"] = Fixtures.GenerateThreeBlobs();

    var memTransport = new MemoryTransport();
    var sentObjectId = await Operations.Send(myObject, new ITransport[] { _transport, memTransport });

    memTransport = new MemoryTransport();
    Base receivedObject = await Operations.Receive(sentObjectId, _transport, memTransport);
    Assert.That(receivedObject, Is.Not.Null);

    var allFiles = Directory
      .GetFiles(_transport.BlobStorageFolder)
      .Select(fp => fp.Split(Path.DirectorySeparatorChar).Last())
      .ToList();
    var blobPaths = allFiles
      .Where(fp => fp.Length > Blob.LocalHashPrefixLength) // excludes things like .DS_store
      .ToList();

    // Check that there are three downloaded blobs!
    Assert.That(blobPaths.Count, Is.EqualTo(3));

    var blobs = ((IList)receivedObject!["blobs"]!).Cast<Blob>().ToList();
    // Check that we have three blobs
    Assert.That(blobs, Has.Count.EqualTo(3));
    // Check that received blobs point to local path (where they were received)
    Assert.That(blobs[0].filePath, Contains.Substring(_transport.BlobStorageFolder));
    Assert.That(blobs[1].filePath, Contains.Substring(_transport.BlobStorageFolder));
    Assert.That(blobs[2].filePath, Contains.Substring(_transport.BlobStorageFolder));
  }

  public void Dispose()
  {
    _client?.Dispose();
    _transport?.Dispose();
  }
}
