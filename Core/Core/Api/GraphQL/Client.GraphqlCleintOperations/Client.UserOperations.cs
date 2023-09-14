#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;

namespace Speckle.Core.Api;

public partial class Client
{
  /// <summary>
  /// Gets the currently active user profile.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<User> ActiveUserGet(CancellationToken cancellationToken = default)
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
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<LimitedUser?> OtherUserGet(string userId, CancellationToken cancellationToken = default)
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
      Variables = new { userId }
    };
    return (await ExecuteGraphQLRequest<LimitedUserData>(request, cancellationToken).ConfigureAwait(false)).otherUser;
  }

  /// <summary>
  /// Searches for a user on the server.
  /// </summary>
  /// <param name="query">String to search for. Must be at least 3 characters</param>
  /// <param name="limit">Max number of users to return</param>
  /// <returns></returns>
  public async Task<List<LimitedUser>> UserSearch(
    string query,
    int limit = 10,
    CancellationToken cancellationToken = default
  )
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
