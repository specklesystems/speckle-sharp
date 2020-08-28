using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Sentry.Protocol;
using Speckle.Core.Logging;

namespace Speckle.Core.Api
{
  public partial class Client
  {
    public delegate void UserStreamCreatedHandler(object sender, object e);
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

        var res = GQLClient.CreateSubscriptionStream<object>(request);
        var subscription = res.Subscribe(response =>
        {
          if (response.Errors != null)
            Log.CaptureAndThrow(new GraphQLException("Could not subscribe to userStreamCreated"), response.Errors);

          if (response.Data != null)
            OnUserStreamCreated(this, response.Data);
        });

      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    public delegate void StreamUpdatedHandler(object sender, object e);
    public event StreamUpdatedHandler OnStreamUpdated;

    public void SubscribeStreamUpdated()
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"subscription { streamUpdated }",
        };

        var res = GQLClient.CreateSubscriptionStream<dynamic>(request);
        var subscription = res.Subscribe(response =>
        {
          if (response.Errors != null)
            Log.CaptureAndThrow(new GraphQLException("Could not subscribe to streamUpdated"), response.Errors);

          if (response.Data != null)
            OnUserStreamCreated(this, response.Data);
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
