#nullable disable
using System;
using GraphQL;
using Speckle.Core.Api.SubscriptionModels;

namespace Speckle.Core.Api;

public partial class Client
{
  #region BranchCreated

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public delegate void BranchCreatedHandler(object sender, BranchInfo e);

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public event BranchCreatedHandler OnBranchCreated;

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public IDisposable BranchCreatedSubscription { get; private set; }

  /// <summary>
  /// Subscribe to events of branch created for a stream
  /// </summary>
  /// <returns></returns>
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public void SubscribeBranchCreated(string streamId)
  {
    var request = new GraphQLRequest { Query = $@"subscription {{ branchCreated (streamId: ""{streamId}"") }}" };

    BranchCreatedSubscription = SubscribeTo<BranchCreatedResult>(
      request,
      (sender, result) => OnBranchCreated?.Invoke(sender, result.branchCreated)
    );
  }

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public bool HasSubscribedBranchCreated => BranchCreatedSubscription != null;

  #endregion


  #region BranchUpdated
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public delegate void BranchUpdatedHandler(object sender, BranchInfo e);

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public event BranchUpdatedHandler OnBranchUpdated;

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public IDisposable BranchUpdatedSubscription { get; private set; }

  /// <summary>
  /// Subscribe to events of branch updated for a stream
  /// </summary>
  /// <returns></returns>
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public void SubscribeBranchUpdated(string streamId, string branchId = null)
  {
    var request = new GraphQLRequest
    {
      Query = $@"subscription {{ branchUpdated (streamId: ""{streamId}"", branchId: ""{branchId}"") }}"
    };
    BranchUpdatedSubscription = SubscribeTo<BranchUpdatedResult>(
      request,
      (sender, result) => OnBranchUpdated?.Invoke(sender, result.branchUpdated)
    );
  }

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public bool HasSubscribedBranchUpdated => BranchUpdatedSubscription != null;

  #endregion

  #region BranchDeleted
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public delegate void BranchDeletedHandler(object sender, BranchInfo e);

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public event BranchDeletedHandler OnBranchDeleted;

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public IDisposable BranchDeletedSubscription { get; private set; }

  /// <summary>
  /// Subscribe to events of branch deleted for a stream
  /// </summary>
  /// <returns></returns>
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public void SubscribeBranchDeleted(string streamId)
  {
    var request = new GraphQLRequest { Query = $@"subscription {{ branchDeleted (streamId: ""{streamId}"") }}" };

    BranchDeletedSubscription = SubscribeTo<BranchDeletedResult>(
      request,
      (sender, result) => OnBranchDeleted?.Invoke(sender, result.branchDeleted)
    );
  }

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public bool HasSubscribedBranchDeleted => BranchDeletedSubscription != null;

  #endregion
}
