using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Sentry;
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
            throw new SpeckleException("Could not subscribe to userStreamAdded", response.Errors);

          if (response.Data != null)
            OnUserStreamAdded(this, response.Data.userStreamAdded);
        });

      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
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
            throw new SpeckleException("Could not subscribe to streamUpdated", response.Errors);

          if (response.Data != null)
            OnStreamUpdated(this, response.Data.streamUpdated);
        });
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
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
            throw new SpeckleException("Could not subscribe to userStreamRemoved", response.Errors);

          if (response.Data != null)
            OnUserStreamRemoved(this, response.Data.userStreamRemoved);
        });
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
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
  }
}
