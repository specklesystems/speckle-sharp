using System.Diagnostics;
using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Transports;
using Tests;

namespace TestsIntegration.Subscriptions;

public class Commits
{
  public Client client;

  private CommitInfo CommitCreatedInfo;
  private CommitInfo CommitDeletedInfo;
  private string commitId;
  private CommitInfo CommitUpdatedInfo;
  private ServerTransport myServerTransport;
  private string streamId;
  public Account testUserAccount;

  [OneTimeSetUp]
  public async Task Setup()
  {
    testUserAccount = await Fixtures.SeedUser().ConfigureAwait(false);
    client = new Client(testUserAccount);
    myServerTransport = new ServerTransport(testUserAccount, null);
    myServerTransport.Api.CompressPayloads = false;
  }

  [Test, Order(0)]
  //[Ignore("Ironically, it fails.")]
  public async Task SubscribeCommitCreated()
  {
    var streamInput = new StreamCreateInput { description = "Hello World", name = "Super Stream 01" };

    streamId = await client.StreamCreate(streamInput).ConfigureAwait(false);
    Assert.NotNull(streamId);

    myServerTransport.StreamId = streamId; // FML

    var branchInput = new BranchCreateInput
    {
      description = "Just testing branch create...",
      name = "awesome-features",
      streamId = streamId
    };

    var branchId = await client.BranchCreate(branchInput).ConfigureAwait(false);
    Assert.NotNull(branchId);

    client.SubscribeCommitCreated(streamId);
    client.OnCommitCreated += Client_OnCommitCreated;

    Thread.Sleep(1000); //let server catch-up

    var myObject = new Base();
    var ptsList = new List<Point>();
    for (int i = 0; i < 100; i++)
      ptsList.Add(new Point(i, i, i));

    myObject["Points"] = ptsList;

    var objectId = await Operations
      .Send(
        myObject,
        new List<ITransport> { myServerTransport },
        false,
        onErrorAction: (name, err) =>
        {
          Debug.WriteLine("Err in transport");
          Debug.WriteLine(err.Message);
        }
      )
      .ConfigureAwait(false);

    var commitInput = new CommitCreateInput
    {
      streamId = streamId,
      branchName = "awesome-features",
      objectId = objectId,
      message = "sending some test points",
      sourceApplication = "Tests",
      totalChildrenCount = 20
    };

    commitId = await client.CommitCreate(commitInput).ConfigureAwait(false);
    Assert.NotNull(commitId);

    await Task.Run(() =>
      {
        Thread.Sleep(2000); //let client catch-up
        Assert.NotNull(CommitCreatedInfo);
        Assert.That(CommitCreatedInfo.message, Is.EqualTo(commitInput.message));
      })
      .ConfigureAwait(false);
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
      id = commitId
    };

    var res = await client.CommitUpdate(commitInput).ConfigureAwait(false);
    Assert.True(res);

    await Task.Run(() =>
      {
        Thread.Sleep(2000); //let client catch-up
        Assert.NotNull(CommitUpdatedInfo);
        Assert.That(CommitUpdatedInfo.message, Is.EqualTo(commitInput.message));
      })
      .ConfigureAwait(false);
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

    var commitInput = new CommitDeleteInput { streamId = streamId, id = commitId };

    var res = await client.CommitDelete(commitInput).ConfigureAwait(false);
    Assert.True(res);

    await Task.Run(() =>
      {
        Thread.Sleep(2000); //let client catch-up
        Assert.NotNull(CommitDeletedInfo);
        Assert.That(CommitDeletedInfo.id, Is.EqualTo(commitId));
      })
      .ConfigureAwait(false);
  }

  private void Client_OnCommitDeleted(object sender, CommitInfo e)
  {
    CommitDeletedInfo = e;
  }
}
