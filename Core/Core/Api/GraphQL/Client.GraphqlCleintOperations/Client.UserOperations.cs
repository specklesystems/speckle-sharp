#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Logging;

namespace Speckle.Core.Api;

public partial class Client
{
  /// <summary>
  /// Gets the currently active user profile.
  /// </summary>
  /// <returns></returns>
  public Task<User> ActiveUserGet()
  {
    return ActiveUserGet(CancellationToken.None);
  }

  /// <summary>
  /// Gets the currently active user profile.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <exception cref="SpeckleException"></exception>
  public async Task<User> ActiveUserGet(CancellationToken cancellationToken)
  {
    var request = new GraphQLRequest
    {
      Query =
        @"query User {
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
                    }"
    };
    return (await ExecuteGraphQLRequest<ActiveUserData>(request, cancellationToken).ConfigureAwait(false)).activeUser;
  }

  /// <summary>
  /// Get another user's profile by its user id.
  /// </summary>
  /// <param name="id">Id of the user you are looking for</param>
  /// <returns></returns>
  public Task<LimitedUser?> OtherUserGet(string id)
  {
    return OtherUserGet(CancellationToken.None, id);
  }

  /// <summary>
  /// Get another user's profile by its user id.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <param name="id">Id of the user you are looking for</param>
  /// <returns></returns>
  /// <exception cref="SpeckleException"></exception>
  public async Task<LimitedUser?> OtherUserGet(CancellationToken cancellationToken, string id)
  {
    var request = new GraphQLRequest
    {
      Query =
        @"query LimitedUser($id: String!) {
                      otherUser(id: $id){
                        id,
                        name,
                        bio,
                        company,
                        avatar,
                        verified,
                        role,
                      }
                    }",
      Variables = new { id }
    };
    return (await ExecuteGraphQLRequest<LimitedUserData>(request, cancellationToken).ConfigureAwait(false)).otherUser;
  }

  /// <summary>
  /// Searches for a user on the server.
  /// </summary>
  /// <param name="query">String to search for. Must be at least 3 characters</param>
  /// <param name="limit">Max number of users to return</param>
  /// <returns></returns>
  public Task<List<LimitedUser>> UserSearch(string query, int limit = 10)
  {
    return UserSearch(CancellationToken.None, query, limit);
  }

  /// <summary>
  /// Searches for a user on the server.
  /// </summary>
  /// <param name="query">String to search for. Must be at least 3 characters</param>
  /// <param name="limit">Max number of users to return</param>
  /// <returns></returns>
  public async Task<List<LimitedUser>> UserSearch(CancellationToken cancellationToken, string query, int limit = 10)
  {
    var request = new GraphQLRequest
    {
      Query =
        @"query UserSearch($query: String!, $limit: Int!) {
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
                    }",
      Variables = new { query, limit }
    };
    return (await ExecuteGraphQLRequest<UserSearchData>(request, cancellationToken).ConfigureAwait(false))
      .userSearch
      .items;
  }
}
