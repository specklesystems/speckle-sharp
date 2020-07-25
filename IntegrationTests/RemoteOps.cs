using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Speckle.Core.Api;
using Speckle.Core.Api.GqlModels;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Tests;

namespace IntegrationTests
{
  public class RemoteOps
  {
    public Remote myRemote;
    private string branchId = "";
    private string branchName = "";
    private string commitId = "";

    [OneTimeSetUp]
    public void Setup()
    {
      myRemote = new Remote(AccountManager.GetAccounts().First());
    }

    [Test]
    public async Task UserGet()
    {
      var res = await myRemote.UserGet();

      Assert.NotNull(res);
    }


    [Test, Order(0)]
    public async Task StreamCreate()
    {
      var res = await myRemote.StreamCreate(new StreamCreateInput
      {
        description = "Hello World",
        name = "Super Stream 01"
      });

      Assert.NotNull(res);
      myRemote.StreamId = res;
    }

    [Test, Order(10)]
    public async Task StreamsGet()
    {
      var res = await myRemote.StreamsGet();

      Assert.NotNull(res);
    }

    [Test, Order(11)]
    public async Task StreamGet()
    {
      var res = await myRemote.StreamGet(myRemote.StreamId);

      Assert.NotNull(res);
      Assert.AreEqual("master", res.branches.items[0].name);
      Assert.IsNotEmpty(res.collaborators);
    }

    [Test, Order(20)]
    public async Task StreamUpdate()
    {
      var res = await myRemote.StreamUpdate(new StreamUpdateInput
      {
        id = myRemote.StreamId,
        description = "Hello World",
        name = "Super Stream 01 EDITED"
      });

      Assert.IsTrue(res);
    }

    [Test, Order(30)]
    public async Task StreamGrantPermission()
    {
      var res = await myRemote.StreamGrantPermission(

        myRemote.StreamId,
        "b4b7f800ac", //TODO: get user id dynamically
        "stream:owner"
      );

      Assert.IsTrue(res);
    }

    [Test, Order(40)]
    public async Task StreamRevokePermission()
    {
      var res = await myRemote.StreamRevokePermission(

        myRemote.StreamId,
        "b4b7f800ac" //TODO: get user id dynamically
      );

      Assert.IsTrue(res);
    }

    #region branches
    [Test, Order(41)]
    public async Task BranchCreate()
    {
      var res = await myRemote.BranchCreate(new BranchCreateInput
      {
        streamId = myRemote.StreamId,
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
      var commit = new Commit();
      for (int i = 0; i < 100; i++)
        commit.Objects.Add(new Point(i, i, i));

      // NOTE:
      // Operations.Upload is designed to be called from the connector, with potentially multiple responses.
      // We could (should?) scaffold a corrolary Remote.Upload() at one point - in beta maybe?
      commitId = await Operations.Upload(commit, remotes: new Remote[] { myRemote });

      var res = await myRemote.CommitCreate(new CommitCreateInput
      {
        streamId = myRemote.StreamId,
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
      var res = await myRemote.CommitUpdate(new CommitUpdateInput
      {
        streamId = myRemote.StreamId,
        id = commitId,
        message = "DIM IS DA BEST"
      });

      Assert.IsTrue(res);
    }
    [Test, Order(45)]
    public async Task CommitDelete()
    {
      var res = await myRemote.CommmitDelete(new CommitDeleteInput
      {
        id = commitId,
        streamId = myRemote.StreamId
      }
      );
      Assert.IsTrue(res);
    }
    #endregion

    [Test, Order(46)]
    public async Task BranchUpdate()
    {
      var res = await myRemote.BranchUpdate(new BranchUpdateInput
      {
        streamId = myRemote.StreamId,
        id = branchId,
        name = "sample-branch EDITED"
      });

      Assert.IsTrue(res);
    }

    [Test, Order(50)]
    public async Task BranchDelete()
    {
      var res = await myRemote.BranchDelete(new BranchDeleteInput
      {
        id = branchId,
        streamId = myRemote.StreamId
      }
      );
      Assert.IsTrue(res);
    }

    #endregion



    [Test, Order(60)]
    public async Task StreamDelete()
    {
      var res = await myRemote.StreamDelete(myRemote.StreamId);
      Assert.IsTrue(res);
    }


  }
}