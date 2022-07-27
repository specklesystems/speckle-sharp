using System;
using System.Collections.Generic;
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
    public async Task SendObjectWithBlob()
    {
      var myObject = Fixtures.GenerateSimpleObject();
      myObject["blob"] = Fixtures.GenerateBlob();
      var objectId = await Operations.Send(myObject, new List<ITransport> { transport });

      var receivedObject = await Operations.Receive(objectId, transport, );
      var cp = receivedObject;
    }
  }
}

