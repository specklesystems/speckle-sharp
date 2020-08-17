using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Tests;

namespace IntegrationTests
{
  public class RemoteOps
  {
    public Client myClient;
    private string streamId = "";
    private string branchId = "";
    private string branchName = "";
    private string commitId = "";

    [OneTimeSetUp]
    public void Setup()
    {
      myClient = new Client(AccountManager.GetAccounts().First());
    }

    [Test]
    public async Task UserGet()
    {
      var res = await myClient.UserGet();

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
      Assert.AreEqual("master", res.branches.items[0].name);
      Assert.IsNotEmpty(res.collaborators);
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

        streamId,
        "b4b7f800ac", //TODO: get user id dynamically
        "stream:owner"
      );

      Assert.IsTrue(res);
    }

    [Test, Order(40)]
    public async Task StreamRevokePermission()
    {
      var res = await myClient.StreamRevokePermission(

        streamId,
        "b4b7f800ac" //TODO: get user id dynamically
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
      myObject["items"] = new List<Point>();
      for (int i = 0; i < 100; i++)
        myObject.GetMemberSafe("items", new List<Point>()).Add(new Point(i, i, i));

      // NOTE:
      // Operations.Upload is designed to be called from the connector, with potentially multiple responses.
      // We could (should?) scaffold a corrolary Remote.Upload() at one point - in beta maybe?
      commitId = await Operations.Send(myObject, streamId, myClient );

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
      var res = await myClient.CommmitDelete(new CommitDeleteInput
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



    [Test, Order(60)]
    public async Task StreamDelete()
    {
      var res = await myClient.StreamDelete(streamId);
      Assert.IsTrue(res);
    }


  }
}
