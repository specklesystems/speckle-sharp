using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;

namespace Speckle.Core.Api;

[SuppressMessage("Design", "CA1068:CancellationToken parameters must come last")]
public partial class Client
{
  #region Stream Grant Permission

  /// <summary>
  /// Grants permissions to a user on a given stream.
  /// </summary>
  /// <param name="permissionInput"></param>
  /// <returns></returns>
  [Obsolete("Please use the `StreamUpdatePermission` method", true)]
  public Task<bool> StreamGrantPermission(StreamPermissionInput permissionInput)
  {
    return StreamGrantPermission(CancellationToken.None, permissionInput);
  }

  /// <summary>
  /// Grants permissions to a user on a given stream.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <param name="permissionInput"></param>
  /// <returns></returns>
  [Obsolete("Please use the `StreamUpdatePermission` method", true)]
  public async Task<bool> StreamGrantPermission(
    CancellationToken cancellationToken,
    StreamPermissionInput permissionInput
  )
  {
    var request = new GraphQLRequest
    {
      Query =
        @"
          mutation streamGrantPermission($permissionParams: StreamGrantPermissionInput!) {
            streamGrantPermission(permissionParams:$permissionParams)
          }",
      Variables = new { permissionParams = permissionInput }
    };

    var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);
    return (bool)res["streamGrantPermission"];
  }

  #endregion

  #region Cancellation token as last param

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<List<ActivityItem>> StreamGetActivity(
    CancellationToken cancellationToken,
    string id,
    DateTime? after = null,
    DateTime? before = null,
    DateTime? cursor = null,
    string actionType = "",
    int limit = 25
  )
  {
    return StreamGetActivity(id, after, before, cursor, actionType, limit, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<List<Branch>> StreamGetBranches(
    CancellationToken cancellationToken,
    string streamId,
    int branchesLimit = 10,
    int commitsLimit = 10
  )
  {
    return StreamGetBranches(streamId, branchesLimit, commitsLimit, CancellationToken.None);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<string> BranchCreate(CancellationToken cancellationToken, BranchCreateInput branchInput)
  {
    return BranchCreate(branchInput, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<Branch> BranchGet(
    CancellationToken cancellationToken,
    string streamId,
    string branchName,
    int commitsLimit = 10
  )
  {
    return BranchGet(streamId, branchName, commitsLimit, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<bool> BranchUpdate(CancellationToken cancellationToken, BranchUpdateInput branchInput)
  {
    return BranchUpdate(branchInput, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<bool> BranchDelete(CancellationToken cancellationToken, BranchDeleteInput branchInput)
  {
    return BranchDelete(branchInput, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<Comments> StreamGetComments(
    CancellationToken cancellationToken,
    string streamId,
    int limit = 25,
    string? cursor = null
  )
  {
    return StreamGetComments(streamId, limit, cursor, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<string> StreamGetCommentScreenshot(CancellationToken cancellationToken, string id, string streamId)
  {
    return StreamGetCommentScreenshot(id, streamId, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<Commit> CommitGet(CancellationToken cancellationToken, string streamId, string commitId)
  {
    return CommitGet(streamId, commitId, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<List<Commit>> StreamGetCommits(CancellationToken cancellationToken, string streamId, int limit = 10)
  {
    return StreamGetCommits(streamId, limit, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<string> CommitCreate(CancellationToken cancellationToken, CommitCreateInput commitInput)
  {
    return CommitCreate(commitInput, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<bool> CommitUpdate(CancellationToken cancellationToken, CommitUpdateInput commitInput)
  {
    return CommitUpdate(commitInput, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<bool> CommitDelete(CancellationToken cancellationToken, CommitDeleteInput commitInput)
  {
    return CommitDelete(commitInput, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<bool> CommitReceived(CancellationToken cancellationToken, CommitReceivedInput commitReceivedInput)
  {
    return CommitReceived(commitReceivedInput, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<SpeckleObject> ObjectGet(CancellationToken cancellationToken, string streamId, string objectId)
  {
    return ObjectGet(streamId, objectId, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<SpeckleObject> ObjectCountGet(CancellationToken cancellationToken, string streamId, string objectId)
  {
    return ObjectCountGet(streamId, objectId, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<Stream> StreamGet(CancellationToken cancellationToken, string id, int branchesLimit = 10)
  {
    return StreamGet(id, branchesLimit, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<List<Stream>> StreamsGet(CancellationToken cancellationToken, int limit = 10)
  {
    return StreamsGet(limit, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<List<Stream>> FavoriteStreamsGet(CancellationToken cancellationToken, int limit = 10)
  {
    return FavoriteStreamsGet(limit, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<List<Stream>> StreamSearch(CancellationToken cancellationToken, string query, int limit = 10)
  {
    return StreamSearch(query, limit, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<string> StreamCreate(CancellationToken cancellationToken, StreamCreateInput streamInput)
  {
    return StreamCreate(streamInput, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<bool> StreamUpdate(CancellationToken cancellationToken, StreamUpdateInput streamInput)
  {
    return StreamUpdate(streamInput, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<bool> StreamDelete(CancellationToken cancellationToken, string id)
  {
    return StreamDelete(id, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<bool> StreamRevokePermission(
    CancellationToken cancellationToken,
    StreamRevokePermissionInput permissionInput
  )
  {
    return StreamRevokePermission(permissionInput, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<Stream> StreamGetPendingCollaborators(CancellationToken cancellationToken, string id)
  {
    return StreamGetPendingCollaborators(id, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<bool> StreamInviteCreate(CancellationToken cancellationToken, StreamInviteCreateInput inviteCreateInput)
  {
    return StreamInviteCreate(inviteCreateInput, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<LimitedUser?> OtherUserGet(CancellationToken cancellationToken, string id)
  {
    return OtherUserGet(id, cancellationToken);
  }

  [Obsolete("Use overload with cancellation token parameter last")]
  public Task<List<LimitedUser>> UserSearch(CancellationToken cancellationToken, string query, int limit = 10)
  {
    return UserSearch(query, limit, cancellationToken);
  }
  #endregion
}
