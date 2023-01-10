using System;
using GraphQL;
using Speckle.Core.Api.SubscriptionModels;
using Speckle.Core.Logging;

namespace Speckle.Core.Api
{
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
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"subscription {{ commitCreated (streamId: ""{streamId}"") }}"
        };

        var res = GQLClient.CreateSubscriptionStream<CommitCreatedResult>(request);
        CommitCreatedSubscription = res.Subscribe(response =>
        {
          if (response.Errors != null)
            throw new SpeckleException("Could not subscribe to commitCreated", response.Errors);

          if (response.Data != null)
            OnCommitCreated?.Invoke(this, response.Data.commitCreated);
        });

      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    public bool HasSubscribedCommitCreated
    {
      get
      {
        return CommitCreatedSubscription != null;
      }
    }
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
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"subscription {{ commitUpdated (streamId: ""{streamId}"", commitId: ""{commitId}"") }}"
        };

        var res = GQLClient.CreateSubscriptionStream<CommitUpdatedResult>(request);
        CommitUpdatedSubscription = res.Subscribe(response =>
        {
          if (response.Errors != null)
            throw new SpeckleException("Could not subscribe to commitUpdated", response.Errors);

          if (response.Data != null)
            OnCommitUpdated(this, response.Data.commitUpdated);
        });

      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    public bool HasSubscribedCommitUpdated
    {
      get
      {
        return CommitUpdatedSubscription != null;
      }
    }
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
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"subscription {{ commitDeleted (streamId: ""{streamId}"") }}"
        };

        var res = GQLClient.CreateSubscriptionStream<CommitDeletedResult>(request);
        CommitDeletedSubscription = res.Subscribe(response =>
        {
          if (response.Errors != null)
            throw new SpeckleException("Could not subscribe to commitDeleted", response.Errors);

          if (response.Data != null)
            OnCommitDeleted(this, response.Data.commitDeleted);
        });

      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    public bool HasSubscribedCommitDeleted
    {
      get
      {
        return CommitDeletedSubscription != null;
      }
    }
    #endregion
  }
}
