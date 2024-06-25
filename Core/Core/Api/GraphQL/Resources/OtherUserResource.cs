using System.Collections.Generic;
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
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<LimitedUser> Get(string id, CancellationToken cancellationToken = default)
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

    var respose = await _client
      .ExecuteGraphQLRequest<LimitedUserResponse>(request, cancellationToken)
      .ConfigureAwait(false);
    return respose.otherUser;
  }

  /// <summary>
  /// Searches for a user on the server.
  /// </summary>
  /// <param name="query">String to search for. Must be at least 3 characters</param>
  /// <param name="limit">Max number of users to return</param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<List<LimitedUser>> UserSearch(
    string query,
    int limit = 10,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
                             query UserSearch($query: String!, $limit: Int!) {
                               userSearch(query: $query, limit: $limit) {
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
    var request = new GraphQLRequest { Query = QUERY, Variables = new { query, limit } };
    return (await _client.ExecuteGraphQLRequest<UserSearchData>(request, cancellationToken).ConfigureAwait(false))
      .userSearch
      .items;
  }
}
