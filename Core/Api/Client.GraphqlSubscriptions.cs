using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Sentry.Protocol;
using Speckle.Core.Logging;
using Speckle.Core.Api.SubscriptionModels;

namespace Speckle.Core.Api
{
  public partial class Client
  {
    #region UserStreamAdded
    public delegate void UserStreamAddedHandler(object sender, StreamInfo e);
    public event UserStreamAddedHandler OnUserStreamAdded;
    public IDisposable UserStreamAddedSubscription;

    /// <summary>
    /// Subscribe to events of streams added for the current user
    /// </summary>
    /// <returns></returns>
    public void SubscribeUserStreamAdded()
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"subscription { userStreamAdded }"
        };

        var res = GQLClient.CreateSubscriptionStream<UserStreamAddedResult>(request);
        UserStreamAddedSubscription = res.Subscribe(response =>
        {
          if (response.Errors != null)
            Log.CaptureAndThrow(new GraphQLException("Could not subscribe to userStreamAdded"), response.Errors);

          if (response.Data != null)
            OnUserStreamAdded(this, response.Data.userStreamAdded);
        });

      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    public bool HasSubscribedUserStreamAdded
    {
      get
      {
        return UserStreamAddedSubscription != null;
      }
    }
    #endregion
    #region StreamUpdated
    public delegate void StreamUpdatedHandler(object sender, StreamInfo e);
    public event StreamUpdatedHandler OnStreamUpdated;
    public IDisposable StreamUpdatedSubscription;

    /// <summary>
    /// Subscribe to events of streams updated for a specific streamId
    /// </summary>
    /// <param name="id">streamId</param>
    public void SubscribeStreamUpdated(string id)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"subscription {{ streamUpdated( streamId: ""{id}"") }}",
        };

        var res = GQLClient.CreateSubscriptionStream<StreamUpdatedResult>(request);
        StreamUpdatedSubscription = res.Subscribe(response =>
        {
          if (response.Errors != null)
            Log.CaptureAndThrow(new GraphQLException("Could not subscribe to streamUpdated"), response.Errors);

          if (response.Data != null)
            OnStreamUpdated(this, response.Data.streamUpdated);
        });
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    public bool HasSubscribedStreamUpdated
    {
      get
      {
        return StreamUpdatedSubscription != null;
      }
    }
    #endregion
    #region StreamRemoved
    public delegate void UserStreamRemovedHandler(object sender, StreamInfo e);
    public event UserStreamRemovedHandler OnUserStreamRemoved;
    public IDisposable UserStreamRemovedSubscription;

    /// <summary>
    /// Subscribe to events of streams removed for the current user
    /// </summary>
    /// <param name="id"></param>
    public void SubscribeUserStreamRemoved()
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"subscription {{ userStreamRemoved }}",
        };

        var res = GQLClient.CreateSubscriptionStream<UserStreamRemovedResult>(request);
        UserStreamRemovedSubscription = res.Subscribe(response =>
        {
          if (response.Errors != null)
            Log.CaptureAndThrow(new GraphQLException("Could not subscribe to userStreamRemoved"), response.Errors);

          if (response.Data != null)
            OnUserStreamRemoved(this, response.Data.userStreamRemoved);
        });
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    public bool HasSubscribedUserStreamRemoved
    {
      get
      {
        return UserStreamRemovedSubscription != null;
      }
    }
    #endregion

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

        var res = GQLClient.CreateSubscriptionStream<BranchEventResult>(request);
        BranchCreatedSubscription = res.Subscribe(response =>
        {
          if (response.Errors != null)
            Log.CaptureAndThrow(new GraphQLException("Could not subscribe to branchCreated"), response.Errors);

          if (response.Data != null)
            OnBranchCreated(this, response.Data.branchCreated);
        });

      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
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
            Log.CaptureAndThrow(new GraphQLException("Could not subscribe to branchUpdated"), response.Errors);

          if (response.Data != null)
            OnBranchUpdated(this, response.Data.branchUpdated);
        });

      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
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
    /// Subscribe to events of branch updated for a stream
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
            Log.CaptureAndThrow(new GraphQLException("Could not subscribe to branchDeleted"), response.Errors);

          if (response.Data != null)
            OnBranchDeleted(this, response.Data.branchDeleted);
        });

      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
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
