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
    /// Subscribe to the UserStreamCreated subscription
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
    /// Subscribe to the StreamUpdated subscription
    /// </summary>
    /// <param name="id"></param>
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
    public delegate void StreamRemovedHandler(object sender, StreamInfo e);
    public event StreamRemovedHandler OnStreamRemoved;
    public IDisposable StreamRemovedSubscription;

    /// <summary>
    /// Subscribe to the StreamDeleted subscription
    /// </summary>
    /// <param name="id"></param>
    public void SubscribeStreamRemoved(string id)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"subscription {{ userStreamRemoved }}",
        };

        var res = GQLClient.CreateSubscriptionStream<UserStreamRemovedResult>(request);
        StreamRemovedSubscription = res.Subscribe(response =>
        {
          if (response.Errors != null)
            Log.CaptureAndThrow(new GraphQLException("Could not subscribe to StreamRemoved"), response.Errors);

          if (response.Data != null)
            OnStreamRemoved(this, response.Data.userStreamRemoved);
        });
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    public bool HasSubscribedStreamRemoved
    {
      get
      {
        return StreamRemovedSubscription != null;
      }
    }
    #endregion
  }
}
