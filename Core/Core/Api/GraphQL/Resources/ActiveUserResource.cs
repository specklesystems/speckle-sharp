using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Models.Responses;

namespace Speckle.Core.Api.GraphQL.Resources;

public sealed class ActiveUserResource
{
  private readonly ISpeckleGraphQLClient _client;

  internal ActiveUserResource(ISpeckleGraphQLClient client)
  {
    _client = client;
  }

  /// <summary>
  /// Gets the currently active user profile.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <returns>the requested user, or null if the user does not exist (i.e. <see cref="Client"/> was initialised with an unauthenticated account)</returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<User?> Get(CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
       query User {
        activeUser {
          id,
          email,
          name,
          bio,
          company,
          avatar,
          verified,
          profiles,
          role,
        }
      }
      """;
    var request = new GraphQLRequest { Query = QUERY };

    var response = await _client
      .ExecuteGraphQLRequest<ActiveUserResponse>(request, cancellationToken)
      .ConfigureAwait(false);

    return response.activeUser;
  }

  /// <param name="limit">Max number of projects to fetch</param>
  /// <param name="cursor">Optional cursor for pagination</param>
  /// <param name="filter">Optional filter</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<ResourceCollection<Project>> GetProjects(
    int limit = ServerLimits.DEFAULT_PAGINATION_REQUEST,
    string? cursor = null,
    UserProjectsFilter? filter = null,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
       query User($limit : Int!, $cursor: String, $filter: UserProjectsFilter) {
        activeUser {
          projects(limit: $limit, cursor: $cursor, filter: $filter) {
             totalCount
             items {
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
          }
        }
      }
      """;
    var request = new GraphQLRequest
    {
      Query = QUERY,
      Variables = new
      {
        limit,
        cursor,
        filter
      }
    };

    var response = await _client
      .ExecuteGraphQLRequest<ActiveUserResponse>(request, cancellationToken)
      .ConfigureAwait(false);

    if (response.activeUser is null)
    {
      throw new SpeckleGraphQLException("GraphQL response indicated that the ActiveUser could not be found");
    }

    return response.activeUser.projects;
  }

  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<List<PendingStreamCollaborator>> ProjectInvites(CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
      query ProjectInvites {
        activeUser {
          projectInvites {
            id
            inviteId
            invitedBy {
              avatar
              bio
              company
              id
              name
              role
              verified
            }
            projectId
            projectName
            role
            streamId
            streamName
            title
            token
            user {
              id,
              name,
              bio,
              company,
              verified,
              role,
            }
          }
        }
      }
      """;

    var request = new GraphQLRequest { Query = QUERY };

    var response = await _client
      .ExecuteGraphQLRequest<ActiveUserResponse>(request, cancellationToken)
      .ConfigureAwait(false);

    if (response.activeUser is null)
    {
      throw new SpeckleGraphQLException("GraphQL response indicated that the ActiveUser could not be found");
    }

    return response.activeUser.projectInvites;
  }
}
