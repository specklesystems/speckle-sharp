using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;

namespace TestsIntegration
{
  public class ServerTransportTests
  {
    public Account account;
    public Client client;
    public ServerTransport transport;
    public string streamId;

    [OneTimeSetUp]
    public void Setup()
    {
      account = Fixtures.SeedUser();
      client = new Client(account);
      streamId = client.StreamCreate(new StreamCreateInput
      {
        description = "Flobber",
        name = "Blobber"
      }).Result;

      transport = new ServerTransport(account, streamId);
    }

    [Test]
    public async Task SendObject()
    {
      var myObject = Fixtures.GenerateNestedObject();

      var objectId = await Operations.Send(myObject, new List<ITransport> { transport });

      var test = objectId;
      Assert.IsNotNull(test);
    }

    [Test]
    public async Task SendAndReceiveObjectWithBlobs()
    {
      var myObject = Fixtures.GenerateSimpleObject();
      myObject["blobs"] = Fixtures.GenerateThreeBlobs();

      var sentObjectId = await Operations.Send(myObject, new List<ITransport> { transport });
      var receivedObject = await Operations.Receive(sentObjectId, transport);

      var allFiles = Directory.GetFiles(transport.BlobStorageFolder)
        .Select(fp => fp.Split(Path.DirectorySeparatorChar).Last()).ToList();
      var blobPaths = allFiles
        .Where(fp => fp.Length > Blob.LocalHashPrefixLength) // excludes things like .DS_store
        .ToList();

      // Check that there are three downloaded blobs! 
      Assert.AreEqual(blobPaths.Count, 3);

      var blobs = (receivedObject["blobs"] as List<object>).Cast<Blob>().ToList();
      // Check that we have three blobs
      Assert.IsTrue(blobs.Count == 3);
      // Check that received blobs point to local path (where they were received)
      Assert.IsTrue(blobs[0].filePath.Contains(transport.BlobStorageFolder));
      Assert.IsTrue(blobs[1].filePath.Contains(transport.BlobStorageFolder));
      Assert.IsTrue(blobs[2].filePath.Contains(transport.BlobStorageFolder));
    }
  }
}

