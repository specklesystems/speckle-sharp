using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;

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
  public async Task<User> Get(CancellationToken cancellationToken = default)
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
      .ExecuteGraphQLRequest<ActiveUserData>(request, cancellationToken)
      .ConfigureAwait(false);

    return response.activeUser;
  }

  public async Task<ResourceCollection<Project>> GetProjects(
    int projectsLimit = 10,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
                          query User($projectsLimit : Int!) {
                           activeUser {
                             projects(limit: $projectsLimit) {
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
    var request = new GraphQLRequest { Query = QUERY, Variables = new { projectsLimit } };

    var response = await _client
      .ExecuteGraphQLRequest<ActiveUserData>(request, cancellationToken)
      .ConfigureAwait(false);

    return response.activeUser.projects;
  }

  public async Task<List<Project>> FilterProjects(
    UserProjectsFilter filter,
    int limit = 10,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
                         query ProjectsFilter($filter: UserProjectsFilter!, $limit: Int!) {
                           activeUser {
                             projects(filter: $filter, limit: $limit) {
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

    var request = new GraphQLRequest { Query = QUERY, Variables = new { filter, limit } };

    var response = await _client
      .ExecuteGraphQLRequest<ActiveUserData>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.activeUser.projects.items;
  }
}
