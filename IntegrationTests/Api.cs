using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Tests;
using System.Net;
using System.Collections.Specialized;
using System.Text;
using Newtonsoft.Json;

////////////////////////////////////////////////////////////////////////////
/// NOTE:                                                                ///
/// These tests don't run without a server running locally.              ///
/// Check out https://github.com/specklesystems/server for               ///
/// more info on the server.                                             ///
////////////////////////////////////////////////////////////////////////////
namespace TestsIntegration
{
  public class Api
  {
    public ServerInfo testServer;
    public Account firstUserAccount, secondUserAccount;

    public Client myClient;
    public ServerTransport myServerTransport;

    private string streamId = "";
    private string branchId = "";
    private string branchName = "";
    private string commitId = "";
    private string objectId = "";

    [OneTimeSetUp]
    public void Setup()
    {
      testServer = new ServerInfo { url = "http://127.0.0.1:3000", name = "TestServer" };

      firstUserAccount = Fixtures.SeedUser(testServer);
      secondUserAccount = Fixtures.SeedUser(testServer);

      myClient = new Client(firstUserAccount);
      myServerTransport = new ServerTransport(firstUserAccount, null);
    }



    [Test]
    public async Task UserGet()
    {
      var res = await myClient.UserGet();

      Assert.NotNull(res);
    }

    [Test]
    public async Task UserSearch()
    {
      var res = await myClient.UserSearch(firstUserAccount.userInfo.email);

      Assert.NotNull(res);
    }


    [Test, Order(0)]
    public async Task StreamCreate()
    {
      var res = await myClient.StreamCreate(new StreamCreateInput
      {
        description = "Hello World",
        name = "Super Stream 01"
      });

      myServerTransport.StreamId = res;
      Assert.NotNull(res);
      streamId = res;
    }

    [Test, Order(10)]
    public async Task StreamsGet()
    {
      var res = await myClient.StreamsGet();

      Assert.NotNull(res);
    }

    [Test, Order(11)]
    public async Task StreamGet()
    {
      var res = await myClient.StreamGet(streamId);

      Assert.NotNull(res);
      Assert.AreEqual("main", res.branches.items[0].name);
      Assert.IsNotEmpty(res.collaborators);
    }

    [Test, Order(12)]
    public async Task StreamSearch()
    {
      var res = await myClient.StreamSearch(streamId);

      Assert.NotNull(res);
    }

    [Test, Order(20)]
    public async Task StreamUpdate()
    {
      var res = await myClient.StreamUpdate(new StreamUpdateInput
      {
        id = streamId,
        description = "Hello World",
        name = "Super Stream 01 EDITED"
      });

      Assert.IsTrue(res);
    }

    [Test, Order(30)]
    public async Task StreamGrantPermission()
    {
      var res = await myClient.StreamGrantPermission(
        new StreamGrantPermissionInput
        {
          streamId = streamId,
          userId = secondUserAccount.userInfo.id,
          role = "stream:owner"
        }
      );

      Assert.IsTrue(res);
    }

    [Test, Order(40)]
    public async Task StreamRevokePermission()
    {
      var res = await myClient.StreamRevokePermission(
        new StreamRevokePermissionInput
        {
          streamId = streamId,
          userId = secondUserAccount.userInfo.id
        }
      );

      Assert.IsTrue(res);
    }

    #region branches
    [Test, Order(41)]
    public async Task BranchCreate()
    {
      var res = await myClient.BranchCreate(new BranchCreateInput
      {
        streamId = streamId,
        description = "this is a sample branch",
        name = "sample-branch"
      });
      Assert.NotNull(res);
      branchId = res;
      branchName = "sample-branch";
    }

    #region commit

    [Test, Order(43)]
    public async Task CommitCreate()
    {
      var myObject = new Base();
      var ptsList = new List<Point>();
      for (int i = 0; i < 100; i++)
        ptsList.Add(new Point(i, i, i));

      myObject["Points"] = ptsList;

      commitId = await Operations.Send(myObject, new List<ITransport>() { myServerTransport }, false);

      var res = await myClient.CommitCreate(new CommitCreateInput
      {
        streamId = streamId,
        branchName = branchName,
        objectId = commitId,
        message = "MATT0E IS THE B3ST"
      });

      Assert.NotNull(res);
      commitId = res;
    }

    [Test, Order(44)]
    public async Task CommitUpdate()
    {
      var res = await myClient.CommitUpdate(new CommitUpdateInput
      {
        streamId = streamId,
        id = commitId,
        message = "DIM IS DA BEST"
      });

      Assert.IsTrue(res);
    }

    [Test, Order(45)]
    public async Task CommitDelete()
    {
      var res = await myClient.CommitDelete(new CommitDeleteInput
      {
        id = commitId,
        streamId = streamId
      }
      );
      Assert.IsTrue(res);
    }
    #endregion

    [Test, Order(46)]
    public async Task BranchUpdate()
    {
      var res = await myClient.BranchUpdate(new BranchUpdateInput
      {
        streamId = streamId,
        id = branchId,
        name = "sample-branch EDITED"
      });

      Assert.IsTrue(res);
    }

    [Test, Order(50)]
    public async Task BranchDelete()
    {
      var res = await myClient.BranchDelete(new BranchDeleteInput
      {
        id = branchId,
        streamId = streamId
      }
      );
      Assert.IsTrue(res);
    }

    #endregion

    #region send/receive bare

    [Test, Order(60)]
    public async Task SendDetached()
    {
      var myObject = new Base();
      var ptsList = new List<Point>();
      for (int i = 0; i < 100; i++)
        ptsList.Add(new Point(i, i, i));

      myObject["@Points"] = ptsList;

      objectId = await Operations.Send(myObject, new List<ITransport>() { myServerTransport });
    }

    [Test, Order(61)]
    public async Task ReceiveAndCompose()
    {
      var myObject = await Operations.Receive(objectId, myServerTransport);
      Assert.NotNull(myObject);
      Assert.AreEqual(100, ((List<object>)myObject["@Points"]).Count);
    }




    #endregion

    [Test, Order(60)]
    public async Task StreamDelete()
    {
      var res = await myClient.StreamDelete(streamId);
      Assert.IsTrue(res);
    }
  }
}
