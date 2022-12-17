using System;
using GraphQL;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Logging;

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
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"subscription {{ branchCreated (streamId: ""{streamId}"") }}"
        };

        var res = GQLClient.CreateSubscriptionStream<BranchCreatedResult>(request);
        BranchCreatedSubscription = res.Subscribe(response =>
        {
          if (response.Errors != null)
            throw new SpeckleException("Could not subscribe to branchCreated", response.Errors);

          if (response.Data != null)
            OnBranchCreated(this, response.Data.branchCreated);
        });

      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    public bool HasSubscribedBranchCreated
    {
      get
      {
        return BranchCreatedSubscription != null;
      }
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
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"subscription {{ branchUpdated (streamId: ""{streamId}"", branchId: ""{branchId}"") }}"
        };

        var res = GQLClient.CreateSubscriptionStream<BranchUpdatedResult>(request);
        BranchUpdatedSubscription = res.Subscribe(response =>
        {
          if (response.Errors != null)
            throw new SpeckleException("Could not subscribe to branchUpdated", response.Errors);

          if (response.Data != null)
            OnBranchUpdated(this, response.Data.branchUpdated);
        });

      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    public bool HasSubscribedBranchUpdated
    {
      get
      {
        return BranchUpdatedSubscription != null;
      }
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
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"subscription {{ branchDeleted (streamId: ""{streamId}"") }}"
        };

        var res = GQLClient.CreateSubscriptionStream<BranchDeletedResult>(request);
        BranchDeletedSubscription = res.Subscribe(response =>
        {
          if (response.Errors != null)
            throw new SpeckleException("Could not subscribe to branchDeleted", response.Errors);

          if (response.Data != null)
            OnBranchDeleted(this, response.Data.branchDeleted);
        });

      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    public bool HasSubscribedBranchDeleted
    {
      get
      {
        return BranchDeletedSubscription != null;
      }
    }
    #endregion
  }
}
