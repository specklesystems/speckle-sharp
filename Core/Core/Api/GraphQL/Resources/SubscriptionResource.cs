using System;
using System.Collections.Generic;
using GraphQL;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Models.Responses;

namespace Speckle.Core.Api.GraphQL.Resources;

public sealed class Subscription<TEventArgs> : IDisposable
  where TEventArgs : EventArgs
{
  internal Subscription(ISpeckleGraphQLClient client, GraphQLRequest request)
  {
    _subscription = client.SubscribeTo<RequiredResponse<TEventArgs>>(request, (o, t) => Listeners?.Invoke(o, t.data));
  }

  public event EventHandler<TEventArgs>? Listeners;

  private readonly IDisposable _subscription;

  public void Dispose()
  {
    _subscription.Dispose();
  }
}

public sealed class SubscriptionResource : IDisposable
{
  private readonly ISpeckleGraphQLClient _client;
  private readonly List<IDisposable> _subscriptions;

  internal SubscriptionResource(ISpeckleGraphQLClient client)
  {
    _client = client;
    _subscriptions = new();
  }

  /// <summary>Track newly added or deleted projects owned by the active user</summary>
  /// <remarks>
  /// You should add event listeners to the returned <see cref="Subscription{TEventArgs}"/> object.<br/>
  /// You can add multiple listeners to a <see cref="Subscription{TEventArgs}"/>, and this should be preferred over creating many subscriptions.<br/>
  /// You should ensure proper disposal of the <see cref="Subscription{TEventArgs}"/> when you're done (see <see cref="IDisposable"/>)<br/>
  /// Disposing of the <see cref="Client"/> or <see cref="SubscriptionResource"/> will also dispose any <see cref="Subscription{TEventArgs}"/>s it created.
  /// </remarks>
  /// <inheritdoc cref="ISpeckleGraphQLClient.SubscribeTo{T}"/>
  public Subscription<UserProjectsUpdatedMessage> CreateUserProjectsUpdatedSubscription()
  {
    //language=graphql
    const string QUERY = """
      subscription UserProjectsUpdated {
        data:userProjectsUpdated {
          id
          project {
            id
          }
          type
        }
      }
      """;
    GraphQLRequest request = new() { Query = QUERY };

    Subscription<UserProjectsUpdatedMessage> subscription = new(_client, request);
    _subscriptions.Add(subscription);
    return subscription;
  }

  /// <summary>Subscribe to updates to resource comments/threads. Optionally specify resource ID string to only receive updates regarding comments for those resources</summary>
  /// <remarks><inheritdoc cref="CreateUserProjectsUpdatedSubscription"/></remarks>
  /// <inheritdoc cref="ISpeckleGraphQLClient.SubscribeTo{T}"/>
  public Subscription<ProjectCommentsUpdatedMessage> CreateProjectCommentsUpdatedSubscription(
    ViewerUpdateTrackingTarget target
  )
  {
    //language=graphql
    const string QUERY = """
      subscription Subscription($target: ViewerUpdateTrackingTarget!) {
        data:projectCommentsUpdated(target: $target) {
          comment {
            id
          }
          id
          type
        }
      }
      """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { target } };

    Subscription<ProjectCommentsUpdatedMessage> subscription = new(_client, request);
    _subscriptions.Add(subscription);
    return subscription;
  }

  /// <summary>Subscribe to changes to a project's models. Optionally specify <paramref name="modelIds"/> to track</summary>
  /// <remarks><inheritdoc cref="CreateUserProjectsUpdatedSubscription"/></remarks>
  /// <inheritdoc cref="ISpeckleGraphQLClient.SubscribeTo{T}"/>
  public Subscription<ProjectModelsUpdatedMessage> CreateProjectModelsUpdatedSubscription(
    string id,
    IReadOnlyList<string>? modelIds = null
  )
  {
    //language=graphql
    const string QUERY = """
      subscription ProjectModelsUpdated($id: String!, $modelIds: [String!]) {
        data:projectModelsUpdated(id: $id, modelIds: $modelIds) {
          id
          model {
            id
            name
            previewUrl
            updatedAt
            description
            displayName
            createdAt
            author {
              avatar
              bio
              company
              id
              name
              role
              totalOwnedStreamsFavorites
              verified
            }
          }
          type
        }
      }
      """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { id, modelIds } };

    Subscription<ProjectModelsUpdatedMessage> subscription = new(_client, request);
    _subscriptions.Add(subscription);
    return subscription;
  }

  /// <summary>Track updates to a specific project</summary>
  /// <remarks><inheritdoc cref="CreateUserProjectsUpdatedSubscription"/></remarks>
  /// <inheritdoc cref="ISpeckleGraphQLClient.SubscribeTo{T}"/>
  public Subscription<ProjectUpdatedMessage> CreateProjectUpdatedSubscription(string id)
  {
    //language=graphql
    const string QUERY = """
      subscription ProjectUpdated($id: String!) {
        data:projectUpdated(id: $id) {
          id
          project {
            id
            name
            description
            visibility
            allowPublicComments
            role
            createdAt
            updatedAt
            sourceApps
          }
          type
        }
      }
      """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { id } };

    Subscription<ProjectUpdatedMessage> subscription = new(_client, request);
    _subscriptions.Add(subscription);
    return subscription;
  }

  /// <summary>Subscribe to changes to a project's versions.</summary>
  /// <remarks><inheritdoc cref="CreateUserProjectsUpdatedSubscription"/></remarks>
  /// <inheritdoc cref="ISpeckleGraphQLClient.SubscribeTo{T}"/>
  public Subscription<ProjectVersionsUpdatedMessage> CreateProjectVersionsUpdatedSubscription(string id)
  {
    //language=graphql
    const string QUERY = """
      subscription ProjectVersionsUpdated($id: String!) {
        data:projectVersionsUpdated(id: $id) {
          id
          modelId
          type
          version {
            id
            referencedObject
            message
            sourceApplication
            createdAt
            previewUrl
            authorUser {
              totalOwnedStreamsFavorites
              id
              name
              bio
              company
              verified
              role
              avatar
            }
            model{
              id
              name
              description
              displayName
              updatedAt
              createdAt
            }
          }
        }
      }
      """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { id } };

    Subscription<ProjectVersionsUpdatedMessage> subscription = new(_client, request);
    _subscriptions.Add(subscription);
    return subscription;
  }

  public void Dispose()
  {
    foreach (var subscription in _subscriptions)
    {
      subscription.Dispose();
    }
  }
}
