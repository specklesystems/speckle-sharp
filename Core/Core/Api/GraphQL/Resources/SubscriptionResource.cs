using System.Runtime.CompilerServices;
using GraphQL;
using Speckle.Core.Api.SubscriptionModels;

namespace Speckle.Core.Api.GraphQL.Resources;

public sealed class SubscriptionResource
{
  private readonly ISpeckleGraphQLSubscriber _client;

  internal SubscriptionResource(ISpeckleGraphQLSubscriber client)
  {
    _client = client;
  }

  private GraphQLSubscription<UserStreamAddedResult> UserProjectAddedSubscription()
  {
    private const string UserProjectsAddedQuery = """
                                                  
                                                    """;
    GraphQLRequest request = new(UserProjectsAddedQuery);
    return new(request,_client);
  }

  private readonly GraphQLSubscription<UserStreamAddedResult> UserProjectsAdded = new(_client, UserProjectsAddedQuery);
  public event 
}
