﻿using System.Diagnostics;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Tests;

namespace TestsIntegration.Subscriptions
{
  public class Commits
  {
    public Client client;
    public Account testUserAccount;

    private CommitInfo CommitCreatedInfo;
    private CommitInfo CommitUpdatedInfo;
    private CommitInfo CommitDeletedInfo;
    private ServerTransport myServerTransport;
    string commitId;
    string streamId;

    [OneTimeSetUp]
    public async Task Setup()
    {
      testUserAccount = await Fixtures.SeedUser();
      client = new Client(testUserAccount);
      myServerTransport = new ServerTransport(testUserAccount, null);
      myServerTransport.Api.CompressPayloads = false;
    }

    [Test, Order(0)]
    //[Ignore("Ironically, it fails.")]
    public async Task SubscribeCommitCreated()
    {
      var streamInput = new StreamCreateInput
      {
        description = "Hello World",
        name = "Super Stream 01"
      };

      streamId = await client.StreamCreate(streamInput);
      Assert.NotNull(streamId);

      myServerTransport.StreamId = streamId; // FML

      var branchInput = new BranchCreateInput
      {
        description = "Just testing branch create...",
        name = "awesome-features",
        streamId = streamId
      };

      var branchId = await client.BranchCreate(branchInput);
      Assert.NotNull(branchId);

      client.SubscribeCommitCreated(streamId);
      client.OnCommitCreated += Client_OnCommitCreated;

      Thread.Sleep(1000); //let server catch-up

      var myObject = new Base();
      var ptsList = new List<Point>();
      for (int i = 0; i < 100; i++)
        ptsList.Add(new Point(i, i, i));

      myObject["Points"] = ptsList;

      var objectId = await Operations.Send(myObject, new List<ITransport>() { myServerTransport }, false, onErrorAction: (name, err) =>
      {
        Debug.WriteLine("Err in transport");
        Debug.WriteLine(err.Message);
      });

      var commitInput = new CommitCreateInput
      {
        streamId = streamId,
        branchName = "awesome-features",
        objectId = objectId,
        message = "sending some test points",
        sourceApplication = "Tests",
        totalChildrenCount = 20
      };

      commitId = await client.CommitCreate(commitInput);
      Assert.NotNull(commitId);

      await Task.Run(() =>
      {
        Thread.Sleep(2000); //let client catch-up
        Assert.NotNull(CommitCreatedInfo);
        Assert.AreEqual(commitInput.message, CommitCreatedInfo.message);
      });
    }

    private void Client_OnCommitCreated(object sender, CommitInfo e)
    {
      CommitCreatedInfo = e;
    }

    [Test, Order(1)]
    //[Ignore("Ironically, it fails.")]
    public async Task SubscribeCommitUpdated()
    {
      client.SubscribeCommitUpdated(streamId);
      client.OnCommitUpdated += Client_OnCommitUpdated;

      Thread.Sleep(1000); //let server catch-up

      var commitInput = new CommitUpdateInput
      {
        message = "Just testing commit update...",
        streamId = streamId,
        id = commitId,
      };

      var res = await client.CommitUpdate(commitInput);
      Assert.True(res);

      await Task.Run(() =>
      {
        Thread.Sleep(2000); //let client catch-up
        Assert.NotNull(CommitUpdatedInfo);
        Assert.AreEqual(commitInput.message, CommitUpdatedInfo.message);
      });
    }

    private void Client_OnCommitUpdated(object sender, CommitInfo e)
    {
      CommitUpdatedInfo = e;
    }

    [Test, Order(3)]
    //[Ignore("Ironically, it fails.")]
    public async Task SubscribeCommitDeleted()
    {
      client.SubscribeCommitDeleted(streamId);
      client.OnCommitDeleted += Client_OnCommitDeleted;

      Thread.Sleep(1000); //let server catch-up

      var commitInput = new CommitDeleteInput
      {
        streamId = streamId,
        id = commitId,
      };

      var res = await client.CommitDelete(commitInput);
      Assert.True(res);

      await Task.Run(() =>
      {
        Thread.Sleep(2000); //let client catch-up
        Assert.NotNull(CommitDeletedInfo);
        Assert.AreEqual(commitId, CommitDeletedInfo.id);
      });
    }

    private void Client_OnCommitDeleted(object sender, CommitInfo e)
    {
      CommitDeletedInfo = e;
    }

  }
}
