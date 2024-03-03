using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Api.GraphQL.Models;

namespace Speckle.Core.Api;

public partial class Client
{
  /// <summary>
  /// Gets the currently active user profile.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  [Obsolete($"Use {nameof(ActiveUser)}")]
  public async Task<User> ActiveUserGet(CancellationToken cancellationToken = default)
  {
    return await ActiveUser.Get(cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// Get another user's profile by its user id.
  /// </summary>
  /// <param name="id">Id of the user you are looking for</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  [Obsolete($"Use {nameof(OtherUser)}")]
  public async Task<LimitedUser?> OtherUserGet(string id, CancellationToken cancellationToken = default)
  {
    return await OtherUser.Get(id, cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// Searches for a user on the server.
  /// </summary>
  /// <param name="query">String to search for. Must be at least 3 characters</param>
  /// <param name="limit">Max number of users to return</param>
  /// <returns></returns>
  [Obsolete($"Use {nameof(OtherUser)}")]
  public async Task<List<LimitedUser>> UserSearch(
    string query,
    int limit = 10,
    CancellationToken cancellationToken = default
  )
  {
    return await OtherUser.UserSearch(query, limit, cancellationToken).ConfigureAwait(false);
  }
}
