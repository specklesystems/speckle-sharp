using System;
using GraphQL;
using Speckle.Core.Api.SubscriptionModels;

// // public partial class Log
// // {

// }

namespace Speckle.Core.Api
{
  public partial class Client
  {
    #region BranchCreated
    public delegate void BranchCreatedHandler(object sender, BranchInfo e);
    public event BranchCreatedHandler OnBranchCreated;
    public IDisposable BranchCreatedSubscription;

    /// <summary>
    /// Subscribe to events of branch created for a stream
    /// </summary>
    /// <returns></returns>
    public void SubscribeBranchCreated(string streamId)
    {
      var request = new GraphQLRequest
      {
        Query = $@"subscription {{ branchCreated (streamId: ""{streamId}"") }}"
      };

      BranchCreatedSubscription = SubscribeTo<BranchCreatedResult>(
        request,
        (sender, result) => OnBranchCreated?.Invoke(sender, result.branchCreated)
      );
    }

    public bool HasSubscribedBranchCreated
    {
      get { return BranchCreatedSubscription != null; }
    }
    #endregion


    #region BranchUpdated
    public delegate void BranchUpdatedHandler(object sender, BranchInfo e);
    public event BranchUpdatedHandler OnBranchUpdated;
    public IDisposable BranchUpdatedSubscription;

    /// <summary>
    /// Subscribe to events of branch updated for a stream
    /// </summary>
    /// <returns></returns>
    public void SubscribeBranchUpdated(string streamId, string branchId = null)
    {
      var request = new GraphQLRequest
      {
        Query =
          $@"subscription {{ branchUpdated (streamId: ""{streamId}"", branchId: ""{branchId}"") }}"
      };
      BranchUpdatedSubscription = SubscribeTo<BranchUpdatedResult>(
        request,
        (sender, result) => OnBranchUpdated?.Invoke(sender, result.branchUpdated)
      );
    }

    public bool HasSubscribedBranchUpdated
    {
      get { return BranchUpdatedSubscription != null; }
    }
    #endregion

    #region BranchDeleted
    public delegate void BranchDeletedHandler(object sender, BranchInfo e);
    public event BranchDeletedHandler OnBranchDeleted;
    public IDisposable BranchDeletedSubscription;

    /// <summary>
    /// Subscribe to events of branch deleted for a stream
    /// </summary>
    /// <returns></returns>
    public void SubscribeBranchDeleted(string streamId)
    {
      var request = new GraphQLRequest
      {
        Query = $@"subscription {{ branchDeleted (streamId: ""{streamId}"") }}"
      };

      BranchDeletedSubscription = SubscribeTo<BranchDeletedResult>(
        request,
        (sender, result) => OnBranchDeleted?.Invoke(sender, result.branchDeleted)
      );
    }

    public bool HasSubscribedBranchDeleted
    {
      get { return BranchDeletedSubscription != null; }
    }
    #endregion
  }
}
