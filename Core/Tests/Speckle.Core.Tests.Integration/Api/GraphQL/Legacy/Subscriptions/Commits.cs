using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;
using Speckle.Core.Models;
using Speckle.Core.Tests.Unit.Kits;
using Speckle.Core.Transports;

namespace Speckle.Core.Tests.Integration.Api.GraphQL.Legacy.Subscriptions;

public class Commits : IDisposable
{
  private Client _client;
  private CommitInfo _commitCreatedInfo;
  private CommitInfo _commitDeletedInfo;
  private string _commitId;
  private CommitInfo _commitUpdatedInfo;
  private ServerTransport _myServerTransport;
  private string _streamId;
  private Account _testUserAccount;

  [OneTimeSetUp]
  public async Task Setup()
  {
    _testUserAccount = await Fixtures.SeedUser();
    _client = new Client(_testUserAccount);
  }

  private void InitServerTransport()
  {
    _myServerTransport = new ServerTransport(_testUserAccount, _streamId);
    _myServerTransport.Api.CompressPayloads = false;
  }

  [Test, Order(0)]
  //[Ignore("Ironically, it fails.")]
  public async Task SubscribeCommitCreated()
  {
    var streamInput = new StreamCreateInput { description = "Hello World", name = "Super Stream 01" };

    _streamId = await _client.StreamCreate(streamInput);
    Assert.That(_streamId, Is.Not.Null);

    InitServerTransport();

    var branchInput = new BranchCreateInput
    {
      description = "Just testing branch create...",
      name = "awesome-features",
      streamId = _streamId
    };

    var branchId = await _client.BranchCreate(branchInput);
    Assert.That(branchId, Is.Not.Null);

    _client.SubscribeCommitCreated(_streamId);
    _client.OnCommitCreated += Client_OnCommitCreated;

    Thread.Sleep(1000); //let server catch-up

    var myObject = new Base();
    var ptsList = new List<Point>();
    for (int i = 0; i < 100; i++)
    {
      ptsList.Add(new Point(i, i, i));
    }

    myObject["Points"] = ptsList;

    var objectId = await Operations.Send(myObject, _myServerTransport, false);

    var commitInput = new CommitCreateInput
    {
      streamId = _streamId,
      branchName = "awesome-features",
      objectId = objectId,
      message = "sending some test points",
      sourceApplication = "Tests",
      totalChildrenCount = 20
    };

    _commitId = await _client.CommitCreate(commitInput);
    Assert.That(_commitId, Is.Not.Null);

    await Task.Run(() =>
    {
      Thread.Sleep(2000); //let client catch-up
      Assert.That(_commitCreatedInfo, Is.Not.Null);
      Assert.That(_commitCreatedInfo.message, Is.EqualTo(commitInput.message));
    });
  }

  private void Client_OnCommitCreated(object sender, CommitInfo e)
  {
    _commitCreatedInfo = e;
  }

  [Test, Order(1)]
  //[Ignore("Ironically, it fails.")]
  public async Task SubscribeCommitUpdated()
  {
    _client.SubscribeCommitUpdated(_streamId);
    _client.OnCommitUpdated += Client_OnCommitUpdated;

    Thread.Sleep(1000); //let server catch-up

    var commitInput = new CommitUpdateInput
    {
      message = "Just testing commit update...",
      streamId = _streamId,
      id = _commitId
    };

    var res = await _client.CommitUpdate(commitInput);
    Assert.That(res, Is.True);

    await Task.Run(() =>
    {
      Thread.Sleep(2000); //let client catch-up
      Assert.That(_commitUpdatedInfo, Is.Not.Null);
      Assert.That(_commitUpdatedInfo.message, Is.EqualTo(commitInput.message));
    });
  }

  private void Client_OnCommitUpdated(object sender, CommitInfo e)
  {
    _commitUpdatedInfo = e;
  }

  [Test, Order(3)]
  //[Ignore("Ironically, it fails.")]
  public async Task SubscribeCommitDeleted()
  {
    _client.SubscribeCommitDeleted(_streamId);
    _client.OnCommitDeleted += Client_OnCommitDeleted;

    Thread.Sleep(1000); //let server catch-up

    var commitInput = new CommitDeleteInput { streamId = _streamId, id = _commitId };

    var res = await _client.CommitDelete(commitInput);
    Assert.That(res, Is.True);

    await Task.Run(() =>
    {
      Thread.Sleep(2000); //let client catch-up
      Assert.That(_commitDeletedInfo, Is.Not.Null);
      Assert.That(_commitDeletedInfo.id, Is.EqualTo(_commitId));
    });
  }

  private void Client_OnCommitDeleted(object sender, CommitInfo e)
  {
    _commitDeletedInfo = e;
  }

  public void Dispose()
  {
    _client?.Dispose();
    _myServerTransport?.Dispose();
  }
}
