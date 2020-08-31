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
    public delegate void UserStreamCreatedHandler(object sender, UserStreamCreatedContent e);
    public event UserStreamCreatedHandler OnUserStreamCreated;

    /// <summary>
    /// Subscribe to the UserStreamCreated event
    /// </summary>
    /// <returns></returns>
    public void SubscribeUserStreamCreated()
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"subscription { userStreamCreated }"
        };

        var res = GQLClient.CreateSubscriptionStream<UserStreamCreatedResult>(request);
        var subscription = res.Subscribe(response =>
        {
          if (response.Errors != null)
            Log.CaptureAndThrow(new GraphQLException("Could not subscribe to userStreamCreated"), response.Errors);

          if (response.Data != null)
            OnUserStreamCreated(this, response.Data.UserStreamCreated);
        });

      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    public delegate void StreamUpdatedHandler(object sender, StreamUpdatedContent e);
    public event StreamUpdatedHandler OnStreamUpdated;

    public void SubscribeStreamUpdated(string id)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"subscription {{ streamUpdated( streamId: ""{id}"") }}",
        };

        var res = GQLClient.CreateSubscriptionStream<StreamUpdatedResult>(request);
        var subscription = res.Subscribe(response =>
        {
          if (response.Errors != null)
            Log.CaptureAndThrow(new GraphQLException("Could not subscribe to streamUpdated"), response.Errors);

          if (response.Data != null)
            OnStreamUpdated(this, response.Data.StreamUpdated);
        });
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }
  }
}
