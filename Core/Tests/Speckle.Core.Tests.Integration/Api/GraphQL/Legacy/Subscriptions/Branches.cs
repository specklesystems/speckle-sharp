using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;

namespace Speckle.Core.Tests.Integration.Api.GraphQL.Legacy.Subscriptions;

public class Branches : IDisposable
{
  private BranchInfo _branchCreatedInfo;
  private BranchInfo _branchDeletedInfo;
  private string _branchId;
  private BranchInfo _branchUpdatedInfo;
  private Client _client;
  private string _streamId;
  private Account _testUserAccount;

  [OneTimeSetUp]
  public async Task Setup()
  {
    _testUserAccount = await Fixtures.SeedUser();
    _client = new Client(_testUserAccount);
  }

  [Test, Order(0)]
  public async Task SubscribeBranchCreated()
  {
    var streamInput = new StreamCreateInput { description = "Hello World", name = "Super Stream 01" };

    _streamId = await _client.StreamCreate(streamInput);
    Assert.That(_streamId, Is.Not.Null);

    _client.SubscribeBranchCreated(_streamId);
    _client.OnBranchCreated += Client_OnBranchCreated;

    Thread.Sleep(5000); //let server catch-up

    var branchInput = new BranchCreateInput
    {
      description = "Just testing branch create...",
      name = "awesome-features",
      streamId = _streamId
    };

    _branchId = await _client.BranchCreate(branchInput);
    Assert.That(_branchId, Is.Not.Null);

    await Task.Run(() =>
    {
      Thread.Sleep(1000); //let client catch-up
      Assert.That(_branchCreatedInfo, Is.Not.Null);
      Assert.That(_branchCreatedInfo.name, Is.EqualTo(branchInput.name));
    });
  }

  private void Client_OnBranchCreated(object sender, BranchInfo e)
  {
    _branchCreatedInfo = e;
  }

  [Test, Order(1)]
  public async Task SubscribeBranchUpdated()
  {
    _client.SubscribeBranchUpdated(_streamId);
    _client.OnBranchUpdated += Client_OnBranchUpdated;

    Thread.Sleep(1000); //let server catch-up

    var branchInput = new BranchUpdateInput
    {
      description = "Just testing branch bpdate...",
      name = "cool-features",
      streamId = _streamId,
      id = _branchId
    };

    var res = await _client.BranchUpdate(branchInput);
    Assert.That(res, Is.True);

    await Task.Run(() =>
    {
      Thread.Sleep(1000); //let client catch-up
      Assert.That(_branchUpdatedInfo, Is.Not.Null);
      Assert.That(_branchUpdatedInfo.name, Is.EqualTo(branchInput.name));
    });
  }

  private void Client_OnBranchUpdated(object sender, BranchInfo e)
  {
    _branchUpdatedInfo = e;
  }

  [Test, Order(3)]
  public async Task SubscribeBranchDeleted()
  {
    _client.SubscribeBranchDeleted(_streamId);
    _client.OnBranchDeleted += Client_OnBranchDeleted;

    Thread.Sleep(1000); //let server catch-up

    var branchInput = new BranchDeleteInput { streamId = _streamId, id = _branchId };

    var res = await _client.BranchDelete(branchInput);
    Assert.That(res, Is.True);

    await Task.Run(() =>
    {
      Thread.Sleep(1000); //let client catch-up
      Assert.That(_branchDeletedInfo, Is.Not.Null);
      Assert.That(_branchDeletedInfo.id, Is.EqualTo(_branchId));
    });
  }

  private void Client_OnBranchDeleted(object sender, BranchInfo e)
  {
    _branchDeletedInfo = e;
  }

  public void Dispose()
  {
    _client?.Dispose();
  }
}
