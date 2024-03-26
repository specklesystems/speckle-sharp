#nullable disable
using System;
using GraphQL;
using Speckle.Core.Api.SubscriptionModels;

namespace Speckle.Core.Api;

public partial class Client
{
  #region CommitCreated

  public delegate void CommitCreatedHandler(object sender, CommitInfo e);

  public event CommitCreatedHandler OnCommitCreated;
  public IDisposable CommitCreatedSubscription;

  /// <summary>
  /// Subscribe to events of commit created for a stream
  /// </summary>
  /// <returns></returns>
  public void SubscribeCommitCreated(string streamId)
  {
    var request = new GraphQLRequest { Query = $@"subscription {{ commitCreated (streamId: ""{streamId}"") }}" };

    CommitCreatedSubscription = SubscribeTo<CommitCreatedResult>(
      request,
      (sender, result) => OnCommitCreated?.Invoke(sender, result.commitCreated)
    );
  }

  public bool HasSubscribedCommitCreated => CommitCreatedSubscription != null;

  #endregion

  #region CommitUpdated

  public delegate void CommitUpdatedHandler(object sender, CommitInfo e);

  public event CommitUpdatedHandler OnCommitUpdated;
  public IDisposable CommitUpdatedSubscription;

  /// <summary>
  /// Subscribe to events of commit updated for a stream
  /// </summary>
  /// <returns></returns>
  public void SubscribeCommitUpdated(string streamId, string commitId = null)
  {
    var request = new GraphQLRequest
    {
      Query = $@"subscription {{ commitUpdated (streamId: ""{streamId}"", commitId: ""{commitId}"") }}"
    };

    var res = GQLClient.CreateSubscriptionStream<CommitUpdatedResult>(request);
    CommitUpdatedSubscription = SubscribeTo<CommitUpdatedResult>(
      request,
      (sender, result) => OnCommitUpdated?.Invoke(sender, result.commitUpdated)
    );
  }

  public bool HasSubscribedCommitUpdated => CommitUpdatedSubscription != null;

  #endregion

  #region CommitDeleted

  public delegate void CommitDeletedHandler(object sender, CommitInfo e);

  public event CommitDeletedHandler OnCommitDeleted;
  public IDisposable CommitDeletedSubscription;

  /// <summary>
  /// Subscribe to events of commit updated for a stream
  /// </summary>
  /// <returns></returns>
  public void SubscribeCommitDeleted(string streamId)
  {
    var request = new GraphQLRequest { Query = $@"subscription {{ commitDeleted (streamId: ""{streamId}"") }}" };
    CommitDeletedSubscription = SubscribeTo<CommitDeletedResult>(
      request,
      (sender, result) => OnCommitDeleted?.Invoke(sender, result.commitDeleted)
    );
  }

  public bool HasSubscribedCommitDeleted => CommitDeletedSubscription != null;

  #endregion
}
