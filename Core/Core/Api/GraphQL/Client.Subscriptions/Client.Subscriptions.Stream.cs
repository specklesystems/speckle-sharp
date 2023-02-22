using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Sentry;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Logging;

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
      var request = new GraphQLRequest { Query = @"subscription { userStreamAdded }" };

      UserStreamAddedSubscription = SubscribeTo<UserStreamAddedResult>(
        request,
        (sender, result) => OnUserStreamAdded?.Invoke(sender, result.userStreamAdded)
      );
    }

    public bool HasSubscribedUserStreamAdded
    {
      get { return UserStreamAddedSubscription != null; }
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
      var request = new GraphQLRequest
      {
        Query = $@"subscription {{ streamUpdated( streamId: ""{id}"") }}",
      };
      StreamUpdatedSubscription = SubscribeTo<StreamUpdatedResult>(
        request,
        (sender, result) => OnStreamUpdated?.Invoke(sender, result.streamUpdated)
      );
    }

    public bool HasSubscribedStreamUpdated
    {
      get { return StreamUpdatedSubscription != null; }
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
      var request = new GraphQLRequest { Query = $@"subscription {{ userStreamRemoved }}", };

      UserStreamRemovedSubscription = SubscribeTo<UserStreamRemovedResult>(
        request,
        (sender, result) => OnUserStreamRemoved?.Invoke(sender, result.userStreamRemoved)
      );
    }

    public bool HasSubscribedUserStreamRemoved
    {
      get { return UserStreamRemovedSubscription != null; }
    }
    #endregion

    #region CommentActivity
    public delegate void CommentActivityHandler(object sender, CommentItem e);
    public event CommentActivityHandler OnCommentActivity;
    public IDisposable CommentActivitySubscription;

    /// <summary>
    /// Subscribe to new comment events
    /// </summary>
    ///
    public void SubscribeCommentActivity(string streamId)
    {
      var request = new GraphQLRequest
      {
        Query =
          $@"subscription {{ commentActivity( streamId: ""{streamId}"") {{ type comment {{ id authorId archived screenshot rawText }} }} }}",
      };
      CommentActivitySubscription = SubscribeTo<CommentActivityResponse>(
        request,
        (sender, result) => OnCommentActivity?.Invoke(sender, result.commentActivity.comment)
      );
    }

    public bool HasSubscribedCommentActivity
    {
      get { return CommentActivitySubscription != null; }
    }
    #endregion
  }
}
