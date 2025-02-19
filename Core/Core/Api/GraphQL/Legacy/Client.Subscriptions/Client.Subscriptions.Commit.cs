#nullable disable
using System;
using GraphQL;
using Speckle.Core.Api.SubscriptionModels;

namespace Speckle.Core.Api;

public partial class Client
{
  #region CommitCreated
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public delegate void CommitCreatedHandler(object sender, CommitInfo e);

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public event CommitCreatedHandler OnCommitCreated;

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public IDisposable CommitCreatedSubscription;

  /// <summary>
  /// Subscribe to events of commit created for a stream
  /// </summary>
  /// <returns></returns>
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public void SubscribeCommitCreated(string streamId)
  {
    var request = new GraphQLRequest { Query = $@"subscription {{ commitCreated (streamId: ""{streamId}"") }}" };

    CommitCreatedSubscription = SubscribeTo<CommitCreatedResult>(
      request,
      (sender, result) => OnCommitCreated?.Invoke(sender, result.commitCreated)
    );
  }

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public bool HasSubscribedCommitCreated => CommitCreatedSubscription != null;

  #endregion

  #region CommitUpdated
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public delegate void CommitUpdatedHandler(object sender, CommitInfo e);

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public event CommitUpdatedHandler OnCommitUpdated;

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public IDisposable CommitUpdatedSubscription;

  /// <summary>
  /// Subscribe to events of commit updated for a stream
  /// </summary>
  /// <returns></returns>
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
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

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public bool HasSubscribedCommitUpdated => CommitUpdatedSubscription != null;

  #endregion

  #region CommitDeleted
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public delegate void CommitDeletedHandler(object sender, CommitInfo e);

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public event CommitDeletedHandler OnCommitDeleted;

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public IDisposable CommitDeletedSubscription;

  /// <summary>
  /// Subscribe to events of commit updated for a stream
  /// </summary>

  /// <returns></returns>
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public void SubscribeCommitDeleted(string streamId)
  {
    var request = new GraphQLRequest { Query = $@"subscription {{ commitDeleted (streamId: ""{streamId}"") }}" };
    CommitDeletedSubscription = SubscribeTo<CommitDeletedResult>(
      request,
      (sender, result) => OnCommitDeleted?.Invoke(sender, result.commitDeleted)
    );
  }

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public bool HasSubscribedCommitDeleted => CommitDeletedSubscription != null;

  #endregion
}
