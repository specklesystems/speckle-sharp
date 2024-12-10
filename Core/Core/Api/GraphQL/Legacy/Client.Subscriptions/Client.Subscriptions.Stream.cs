#nullable disable
using System;
using GraphQL;
using Speckle.Core.Api.SubscriptionModels;

namespace Speckle.Core.Api;

public partial class Client
{
  #region UserStreamAdded
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public delegate void UserStreamAddedHandler(object sender, StreamInfo e);

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public event UserStreamAddedHandler OnUserStreamAdded;

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public IDisposable UserStreamAddedSubscription;

  /// <summary>
  /// Subscribe to events of streams added for the current user
  /// </summary>
  /// <returns></returns>
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public void SubscribeUserStreamAdded()
  {
    var request = new GraphQLRequest { Query = @"subscription { userStreamAdded }" };

    UserStreamAddedSubscription = SubscribeTo<UserStreamAddedResult>(
      request,
      (sender, result) => OnUserStreamAdded?.Invoke(sender, result.userStreamAdded)
    );
  }

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public bool HasSubscribedUserStreamAdded => UserStreamAddedSubscription != null;

  #endregion

  #region StreamUpdated
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public delegate void StreamUpdatedHandler(object sender, StreamInfo e);

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public event StreamUpdatedHandler OnStreamUpdated;

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public IDisposable StreamUpdatedSubscription;

  /// <summary>
  /// Subscribe to events of streams updated for a specific streamId
  /// </summary>
  /// <param name="id">streamId</param>
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public void SubscribeStreamUpdated(string id)
  {
    var request = new GraphQLRequest { Query = $@"subscription {{ streamUpdated( streamId: ""{id}"") }}" };
    StreamUpdatedSubscription = SubscribeTo<StreamUpdatedResult>(
      request,
      (sender, result) => OnStreamUpdated?.Invoke(sender, result.streamUpdated)
    );
  }

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public bool HasSubscribedStreamUpdated => StreamUpdatedSubscription != null;

  #endregion

  #region StreamRemoved
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public delegate void UserStreamRemovedHandler(object sender, StreamInfo e);

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public event UserStreamRemovedHandler OnUserStreamRemoved;

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public IDisposable UserStreamRemovedSubscription;

  /// <summary>
  /// Subscribe to events of streams removed for the current user
  /// </summary>
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public void SubscribeUserStreamRemoved()
  {
    var request = new GraphQLRequest { Query = @"subscription { userStreamRemoved }" };

    UserStreamRemovedSubscription = SubscribeTo<UserStreamRemovedResult>(
      request,
      (sender, result) => OnUserStreamRemoved?.Invoke(sender, result.userStreamRemoved)
    );
  }

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public bool HasSubscribedUserStreamRemoved => UserStreamRemovedSubscription != null;

  #endregion

  #region CommentActivity
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public delegate void CommentActivityHandler(object sender, CommentItem e);

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public event CommentActivityHandler OnCommentActivity;

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE)]
  public IDisposable CommentActivitySubscription;

  /// <summary>
  /// Subscribe to new comment events
  /// </summary>
  ///
  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public void SubscribeCommentActivity(string streamId)
  {
    var request = new GraphQLRequest
    {
      Query =
        $@"subscription {{ commentActivity( streamId: ""{streamId}"") {{ type comment {{ id authorId archived screenshot rawText }} }} }}"
    };
    CommentActivitySubscription = SubscribeTo<CommentActivityResponse>(
      request,
      (sender, result) => OnCommentActivity?.Invoke(sender, result.commentActivity.comment)
    );
  }

  [Obsolete(DeprecationMessages.FE1_DEPRECATION_MESSAGE, true)]
  public bool HasSubscribedCommentActivity => CommentActivitySubscription != null;

  #endregion
}
