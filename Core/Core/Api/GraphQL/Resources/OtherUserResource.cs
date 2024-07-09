using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Models.Responses;

namespace Speckle.Core.Api.GraphQL.Resources;

public sealed class OtherUserResource
{
  private readonly ISpeckleGraphQLClient _client;

  internal OtherUserResource(ISpeckleGraphQLClient client)
  {
    _client = client;
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="id"></param>
  /// <param name="cancellationToken"></param>
  /// <returns>the requested user, or null if the user does not exist</returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<LimitedUser?> Get(string id, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
                         query LimitedUser($id: String!) {
                           otherUser(id: $id){
                             id,
                             name,
                             bio,
                             company,
                             avatar,
                             verified,
                             role,
                           }
                         }
                         """;

    var request = new GraphQLRequest { Query = QUERY, Variables = new { id } };

    var response = await _client
      .ExecuteGraphQLRequest<LimitedUserResponse>(request, cancellationToken)
      .ConfigureAwait(false);

    return response.otherUser;
  }

  /// <summary>
  /// Searches for a user on the server.
  /// </summary>
  /// <param name="query">String to search for. Must be at least 3 characters</param>
  /// <param name="limit">Max number of users to fetch</param>
  /// <param name="cursor">Optional cursor for pagination</param>
  /// <param name="archived"></param>
  /// <param name="emailOnly"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<ResourceCollection<LimitedUser>> UserSearch(
    string query,
    int limit = ServerLimits.DEFAULT_PAGINATION_REQUEST,
    string? cursor = null,
    bool archived = false,
    bool emailOnly = false,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
                             query UserSearch($query: String!, $limit: Int!, $cursor: String, $archived: Boolean, $emailOnly: Boolean) {
                               userSearch(query: $query, limit: $limit, cursor: $cursor, archived: $archived, emailOnly: $emailOnly) {
                                 cursor,
                                 items {
                                  id
                                  name
                                  bio
                                  company
                                  avatar
                                  verified
                                  role
                                }
                              }
                             }
                             """;

    var request = new GraphQLRequest
    {
      Query = QUERY,
      Variables = new
      {
        query,
        limit,
        cursor,
        archived,
        emailOnly
      }
    };

    var response = await _client
      .ExecuteGraphQLRequest<UserSearchResponse>(request, cancellationToken)
      .ConfigureAwait(false);

    return response.userSearch;
  }
}
