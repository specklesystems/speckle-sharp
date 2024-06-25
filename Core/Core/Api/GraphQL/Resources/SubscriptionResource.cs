using System;
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
    // _userProjectsAdded = UserProjectAddedSubscription();
  }

  // private readonly GraphQLSubscription<UserStreamAddedResult> _userProjectsAdded;
  // public event Action<object, UserStreamAddedResult> UserProjectsAdded
  // {
  //   add => _userProjectsAdded.Callback += value;
  //   remove => _userProjectsAdded.Callback -= value;
  // }
  //
  private GraphQLSubscription<UserStreamAddedResult> UserProjectAddedSubscription()
  {
    const string UserProjectsAddedQuery = """
  
                                          """;
    GraphQLRequest request = new(UserProjectsAddedQuery);
    return new GraphQLSubscription<UserStreamAddedResult>(request, _client);
  }
}
