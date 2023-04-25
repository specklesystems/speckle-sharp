using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;

namespace TestsIntegration.Subscriptions;

public class Branches
{
  private BranchInfo BranchCreatedInfo;
  private BranchInfo BranchDeletedInfo;
  private string branchId;
  private BranchInfo BranchUpdatedInfo;
  public Client client;
  private string streamId;
  public Account testUserAccount;

  [OneTimeSetUp]
  public async Task Setup()
  {
    testUserAccount = await Fixtures.SeedUser().ConfigureAwait(false);
    client = new Client(testUserAccount);
  }

  [Test, Order(0)]
  public async Task SubscribeBranchCreated()
  {
    var streamInput = new StreamCreateInput { description = "Hello World", name = "Super Stream 01" };

    streamId = await client.StreamCreate(streamInput).ConfigureAwait(false);
    Assert.NotNull(streamId);

    client.SubscribeBranchCreated(streamId);
    client.OnBranchCreated += Client_OnBranchCreated;

    Thread.Sleep(5000); //let server catch-up

    var branchInput = new BranchCreateInput
    {
      description = "Just testing branch create...",
      name = "awesome-features",
      streamId = streamId
    };

    branchId = await client.BranchCreate(branchInput).ConfigureAwait(false);
    Assert.NotNull(branchId);

    await Task.Run(() =>
      {
        Thread.Sleep(1000); //let client catch-up
        Assert.NotNull(BranchCreatedInfo);
        Assert.That(BranchCreatedInfo.name, Is.EqualTo(branchInput.name));
      })
      .ConfigureAwait(false);
  }

  private void Client_OnBranchCreated(object sender, BranchInfo e)
  {
    BranchCreatedInfo = e;
  }

  [Test, Order(1)]
  public async Task SubscribeBranchUpdated()
  {
    client.SubscribeBranchUpdated(streamId);
    client.OnBranchUpdated += Client_OnBranchUpdated;

    Thread.Sleep(1000); //let server catch-up

    var branchInput = new BranchUpdateInput
    {
      description = "Just testing branch bpdate...",
      name = "cool-features",
      streamId = streamId,
      id = branchId
    };

    var res = await client.BranchUpdate(branchInput).ConfigureAwait(false);
    Assert.True(res);

    await Task.Run(() =>
      {
        Thread.Sleep(1000); //let client catch-up
        Assert.NotNull(BranchUpdatedInfo);
        Assert.That(BranchUpdatedInfo.name, Is.EqualTo(branchInput.name));
      })
      .ConfigureAwait(false);
  }

  private void Client_OnBranchUpdated(object sender, BranchInfo e)
  {
    BranchUpdatedInfo = e;
  }

  [Test, Order(3)]
  public async Task SubscribeBranchDeleted()
  {
    client.SubscribeBranchDeleted(streamId);
    client.OnBranchDeleted += Client_OnBranchDeleted;

    Thread.Sleep(1000); //let server catch-up

    var branchInput = new BranchDeleteInput { streamId = streamId, id = branchId };

    var res = await client.BranchDelete(branchInput).ConfigureAwait(false);
    Assert.True(res);

    await Task.Run(() =>
      {
        Thread.Sleep(1000); //let client catch-up
        Assert.NotNull(BranchDeletedInfo);
        Assert.That(BranchDeletedInfo.id, Is.EqualTo(branchId));
      })
      .ConfigureAwait(false);
  }

  private void Client_OnBranchDeleted(object sender, BranchInfo e)
  {
    BranchDeletedInfo = e;
  }
}
