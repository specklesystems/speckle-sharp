using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Resources;

namespace Speckle.Core.Api;

public partial class Client
{
  /// <summary>
  /// Gets the currently active user profile.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <seealso cref="ActiveUserResource.Get"/>
  [Obsolete($"Use client.{nameof(ActiveUser)}.{nameof(ActiveUserResource.Get)}")]
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
  /// <seealso cref="OtherUserResource.Get"/>
  [Obsolete($"Use client.{nameof(OtherUser)}.{nameof(OtherUserResource.Get)}")]
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
  /// <seealso cref="OtherUserResource.Get"/>
  [Obsolete($"Use client.{nameof(OtherUser)}.{nameof(OtherUserResource.UserSearch)}")]
  public async Task<List<LimitedUser>> UserSearch(
    string query,
    int limit = 10,
    CancellationToken cancellationToken = default
  )
  {
    var res = await OtherUser.UserSearch(query, limit, cancellationToken: cancellationToken).ConfigureAwait(false);
    return res.items;
  }
}
