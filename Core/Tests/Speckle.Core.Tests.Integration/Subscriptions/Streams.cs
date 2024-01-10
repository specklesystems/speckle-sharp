using Speckle.Core.Api;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Credentials;

namespace Speckle.Core.Tests.Integration.Subscriptions;

public class Streams : IDisposable
{
  private Client _client;

  private StreamInfo _streamAddedInfo;
  private string _streamId;
  private StreamInfo _streamRemovedInfo;
  private StreamInfo _streamUpdatedInfo;
  private Account _testUserAccount;

  [OneTimeSetUp]
  public async Task Setup()
  {
    _testUserAccount = await Fixtures.SeedUser().ConfigureAwait(false);
    _client = new Client(_testUserAccount);
  }

  [Test, Order(0)]
  public async Task SubscribeStreamAdded()
  {
    _client.SubscribeUserStreamAdded();
    _client.OnUserStreamAdded += Client_OnUserStreamAdded;

    Thread.Sleep(1000); //let server catch-up

    var streamInput = new StreamCreateInput { description = "Hello World", name = "Super Stream 01" };

    var res = await _client.StreamCreate(streamInput).ConfigureAwait(true);
    _streamId = res;
    Assert.NotNull(res);

    await Task.Run(() =>
      {
        Thread.Sleep(1000); //let client catch-up
        Assert.NotNull(_streamAddedInfo);
        Assert.That(_streamAddedInfo.name, Is.EqualTo(streamInput.name));
      })
      .ConfigureAwait(false);
  }

  private void Client_OnUserStreamAdded(object sender, StreamInfo e)
  {
    _streamAddedInfo = e;
  }

  [Test, Order(1)]
  public async Task SubscribeStreamUpdated()
  {
    _client.SubscribeStreamUpdated(_streamId);
    _client.OnStreamUpdated += Client_OnStreamUpdated;
    ;

    Thread.Sleep(100); //let server catch-up

    var streamInput = new StreamUpdateInput
    {
      id = _streamId,
      description = "Hello World",
      name = "Super Stream 01 EDITED"
    };

    var res = await _client.StreamUpdate(streamInput).ConfigureAwait(false);

    Assert.True(res);

    await Task.Run(() =>
      {
        Thread.Sleep(100); //let client catch-up
        Assert.NotNull(_streamUpdatedInfo);
        Assert.That(_streamUpdatedInfo.name, Is.EqualTo(streamInput.name));
      })
      .ConfigureAwait(false);
  }

  private void Client_OnStreamUpdated(object sender, StreamInfo e)
  {
    _streamUpdatedInfo = e;
  }

  [Test, Order(2)]
  public async Task SubscribeUserStreamRemoved()
  {
    _client.SubscribeUserStreamRemoved();
    _client.OnUserStreamRemoved += Client_OnStreamRemoved;
    ;

    Thread.Sleep(100); //let server catch-up

    var res = await _client.StreamDelete(_streamId).ConfigureAwait(false);

    Assert.True(res);

    await Task.Run(() =>
      {
        Thread.Sleep(100); //let client catch-up
        Assert.NotNull(_streamRemovedInfo);
        Assert.That(_streamRemovedInfo.id, Is.EqualTo(_streamId));
      })
      .ConfigureAwait(false);
  }

  private void Client_OnStreamRemoved(object sender, StreamInfo e)
  {
    _streamRemovedInfo = e;
  }

  public void Dispose()
  {
    _client?.Dispose();
  }
}
