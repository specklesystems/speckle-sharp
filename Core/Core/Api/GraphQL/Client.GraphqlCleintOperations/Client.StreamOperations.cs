using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Logging;

namespace Speckle.Core.Api;

public partial class Client
{

  /// <summary>
  /// Cheks if a stream exists by id.
  /// </summary>
  /// <param name="id">Id of the stream to get</param>
  /// <returns></returns>
  public async Task<bool> IsStreamAccessible(string id, CancellationToken cancellationToken = default)
  {
    try
    {
      var request = new GraphQLRequest
      {
        Query =
          $@"query Stream($id: String!) {{
                      stream(id: $id) {{
                        id
                      }}
                    }}",
        Variables = new { id }
      };
      var stream = (await ExecuteGraphQLRequest<StreamData>(request, cancellationToken).ConfigureAwait(false)).stream;

      return stream.id == id;
    }
    catch (SpeckleGraphQLForbiddenException<StreamData>)
    {
      return false;
    }
    catch (SpeckleGraphQLStreamNotFoundException<StreamData>)
    {
      return false;
    }

  }


  /// <summary>
  /// Gets a stream by id including basic branch info (id, name, description, and total commit count).
  /// For detailed commit and branch info, use StreamGetCommits and StreamGetBranches respectively.
  /// </summary>
  /// <param name="id">Id of the stream to get</param>
  /// <param name="branchesLimit">Max number of branches to retrieve</param>
  /// <returns></returns>
  public Task<Stream> StreamGet(string id, int branchesLimit = 10)
  {
    return StreamGet(CancellationToken.None, id, branchesLimit);
  }

  /// <summary>
  /// Gets a stream by id including basic branch info (id, name, description, and total commit count).
  /// For detailed commit and branch info, use StreamGetCommits and StreamGetBranches respectively.
  /// </summary>
  /// <param name="id">Id of the stream to get</param>
  /// <param name="branchesLimit">Max number of branches to retrieve</param>
  /// <returns></returns>
  public async Task<Stream> StreamGet(CancellationToken cancellationToken, string id, int branchesLimit = 10)
  {
    var request = new GraphQLRequest
    {
      Query =
        $@"query Stream($id: String!) {{
                      stream(id: $id) {{
                        id
                        name
                        description
                        isPublic
                        role
                        createdAt
                        updatedAt
                        commentCount
                        favoritedDate
                        favoritesCount
                        collaborators {{
                          id
                          name
                          role
                          avatar
                        }},
                        branches (limit: {branchesLimit}){{
                          totalCount,
                          cursor,
                          items {{
                            id,
                            name,
                            description,
                            commits {{
                              totalCount
                            }}
                          }}
                        }}
                      }}
                    }}",
      Variables = new { id }
    };
    return (await ExecuteGraphQLRequest<StreamData>(request, cancellationToken).ConfigureAwait(false)).stream;
  }

  /// <summary>
  /// Gets all streams for the current user
  /// </summary>
  /// <param name="limit">Max number of streams to return</param>
  /// <returns></returns>
  public Task<List<Stream>> StreamsGet(int limit = 10)
  {
    return StreamsGet(CancellationToken.None, limit);
  }

  /// <summary>
  /// Gets all streams for the current user
  /// </summary>
  /// <param name="limit">Max number of streams to return</param>
  /// <returns></returns>
  public async Task<List<Stream>> StreamsGet(CancellationToken cancellationToken, int limit = 10)
  {
    var request = new GraphQLRequest
    {
      Query =
        $@"query User {{
                      activeUser{{
                        id,
                        email,
                        name,
                        bio,
                        company,
                        avatar,
                        verified,
                        profiles,
                        role,
                        streams(limit:{limit}) {{
                          totalCount,
                          cursor,
                          items {{
                            id,
                            name,
                            description,
                            isPublic,
                            role,
                            createdAt,
                            updatedAt,
                            favoritedDate,
                            commentCount
                            favoritesCount
                            collaborators {{
                              id,
                              name,
                              role,
                              avatar
                            }}
                          }}
                        }}
                      }}
                    }}"
    };

    var res = await ExecuteGraphQLRequest<ActiveUserData>(request, cancellationToken).ConfigureAwait(false);

    if (res?.activeUser == null)
      throw new SpeckleException(
        "User is not authenticated, or the credentials were not valid. Check the provided account is still valid, remove it from manager and add it again."
      );
    return res.activeUser.streams.items;
  }

  public Task<List<Stream>> FavoriteStreamsGet(int limit = 10)
  {
    return FavoriteStreamsGet(CancellationToken.None, limit);
  }

  /// <summary>
  /// Gets all favorite streams for the current user
  /// </summary>
  /// <param name="limit">Max number of streams to return</param>
  /// <returns></returns>
  public async Task<List<Stream>> FavoriteStreamsGet(CancellationToken cancellationToken, int limit = 10)
  {
    var request = new GraphQLRequest
    {
      Query =
        $@"query User {{
                      activeUser{{
                        id,
                        email,
                        name,
                        bio,
                        company,
                        avatar,
                        verified,
                        profiles,
                        role,
                        favoriteStreams(limit:{limit}) {{
                          totalCount,
                          cursor,
                          items {{
                            id,
                            name,
                            description,
                            isPublic,
                            role,
                            createdAt,
                            updatedAt,
                            favoritedDate,
                            commentCount
                            favoritesCount
                            collaborators {{
                              id,
                              name,
                              role,
                              avatar
                            }}
                          }}
                        }}
                      }}
                    }}"
    };
    return (await ExecuteGraphQLRequest<ActiveUserData>(request, cancellationToken).ConfigureAwait(false))
      .activeUser
      .favoriteStreams
      .items;
  }

  /// <summary>
  /// Searches the user's streams by name, description, and ID
  /// </summary>
  /// <param name="query">String query to search for</param>
  /// <param name="limit">Max number of streams to return</param>
  /// <returns></returns>
  public Task<List<Stream>> StreamSearch(string query, int limit = 10)
  {
    return StreamSearch(CancellationToken.None, query, limit);
  }

  /// <summary>
  /// Searches the user's streams by name, description, and ID
  /// </summary>
  /// <param name="query">String query to search for</param>
  /// <param name="limit">Max number of streams to return</param>
  /// <returns></returns>
  public async Task<List<Stream>> StreamSearch(CancellationToken cancellationToken, string query, int limit = 10)
  {
    var request = new GraphQLRequest
    {
      Query =
        @"query Streams ($query: String!, $limit: Int!) {
                      streams(query: $query, limit: $limit) {
                        totalCount,
                        cursor,
                        items {
                          id,
                          name,
                          description,
                          isPublic,
                          role,
                          createdAt,
                          updatedAt,
                          commentCount
                          favoritesCount
                          collaborators {
                            id,
                            name,
                            role
                          }
                        }
                      }     
                    }",
      Variables = new { query, limit }
    };

    var res = await GQLClient.SendMutationAsync<StreamsData>(request, cancellationToken).ConfigureAwait(false);
    return (await ExecuteGraphQLRequest<StreamsData>(request, cancellationToken).ConfigureAwait(false)).streams.items;
  }

  /// <summary>
  /// Creates a stream.
  /// </summary>
  /// <param name="streamInput"></param>
  /// <returns>The stream's id.</returns>
  public Task<string> StreamCreate(StreamCreateInput streamInput)
  {
    return StreamCreate(CancellationToken.None, streamInput);
  }

  /// <summary>
  /// Creates a stream.
  /// </summary>
  /// <param name="streamInput"></param>
  /// <returns>The stream's id.</returns>
  public async Task<string> StreamCreate(CancellationToken cancellationToken, StreamCreateInput streamInput)
  {
    var request = new GraphQLRequest
    {
      Query = @"mutation streamCreate($myStream: StreamCreateInput!) { streamCreate(stream: $myStream) }",
      Variables = new { myStream = streamInput }
    };
    var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);
    return (string)res["streamCreate"];
  }

  /// <summary>
  /// Updates a stream.
  /// </summary>
  /// <param name="streamInput">Note: the id field needs to be a valid stream id.</param>
  /// <returns>The stream's id.</returns>
  public Task<bool> StreamUpdate(StreamUpdateInput streamInput)
  {
    return StreamUpdate(CancellationToken.None, streamInput);
  }

  /// <summary>
  /// Updates a stream.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <param name="streamInput">Note: the id field needs to be a valid stream id.</param>
  /// <returns>The stream's id.</returns>
  public async Task<bool> StreamUpdate(CancellationToken cancellationToken, StreamUpdateInput streamInput)
  {
    var request = new GraphQLRequest
    {
      Query = @"mutation streamUpdate($myStream: StreamUpdateInput!) { streamUpdate(stream:$myStream) }",
      Variables = new { myStream = streamInput }
    };

    var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);

    return (bool)res["streamUpdate"];
  }

  /// <summary>
  /// Deletes a stream.
  /// </summary>
  /// <param name="id">Id of the stream to be deleted</param>
  /// <returns></returns>
  public Task<bool> StreamDelete(string id)
  {
    return StreamDelete(CancellationToken.None, id);
  }

  /// <summary>
  /// Deletes a stream.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <param name="id">Id of the stream to be deleted</param>
  /// <returns></returns>
  public async Task<bool> StreamDelete(CancellationToken cancellationToken, string id)
  {
    var request = new GraphQLRequest
    {
      Query = @"mutation streamDelete($id: String!) { streamDelete(id:$id) }",
      Variables = new { id }
    };
    var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);
    return (bool)res["streamDelete"];
  }

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

  /// <summary>
  /// Revokes permissions of a user on a given stream.
  /// </summary>
  /// <param name="permissionInput"></param>
  /// <returns></returns>
  public Task<bool> StreamRevokePermission(StreamRevokePermissionInput permissionInput)
  {
    return StreamRevokePermission(CancellationToken.None, permissionInput);
  }

  /// <summary>
  /// Revokes permissions of a user on a given stream.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <param name="permissionInput"></param>
  /// <returns></returns>
  public async Task<bool> StreamRevokePermission(
    CancellationToken cancellationToken,
    StreamRevokePermissionInput permissionInput
  )
  {
    var request = new GraphQLRequest
    {
      Query =
        @"mutation streamRevokePermission($permissionParams: StreamRevokePermissionInput!) {
            streamRevokePermission(permissionParams: $permissionParams)
          }",
      Variables = new { permissionParams = permissionInput }
    };

    var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);
    return (bool)res["streamRevokePermission"];
  }

  /// <summary>
  /// Updates permissions for a user on a given stream.
  /// </summary>
  /// <param name="updatePermissionInput">includes the streamId, the userId of the user to update, and the user's new role</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <exception cref="SpeckleException"></exception>
  public async Task<bool> StreamUpdatePermission(
    StreamPermissionInput updatePermissionInput,
    CancellationToken cancellationToken = default
  )
  {
    var request = new GraphQLRequest
    {
      Query =
        @"
            mutation streamUpdatePermission($permissionParams: StreamUpdatePermissionInput!) {
              streamUpdatePermission(permissionParams:$permissionParams)
            }",
      Variables = new { permissionParams = updatePermissionInput }
    };

    var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);
    return (bool)res["streamUpdatePermission"];
  }

  /// <summary>
  /// Gets the pending collaborators of a stream by id.
  /// Requires the user to be an owner of the stream.
  /// </summary>
  /// <param name="id">Id of the stream to get</param>
  /// <returns></returns>
  public Task<Stream> StreamGetPendingCollaborators(string id)
  {
    return StreamGetPendingCollaborators(CancellationToken.None, id);
  }

  /// <summary>
  /// Gets the pending collaborators of a stream by id.
  /// Requires the user to be an owner of the stream.
  /// </summary>
  /// <param name="id">Id of the stream to get</param>
  /// <param name="branchesLimit">Max number of branches to retrieve</param>
  /// <returns></returns>
  public async Task<Stream> StreamGetPendingCollaborators(CancellationToken cancellationToken, string id)
  {
    var request = new GraphQLRequest
    {
      Query =
        @"query Stream($id: String!) {
                      stream(id: $id) {
                        id
                        pendingCollaborators {
                          id
                          inviteId
                          title
                          role
                          user {
                            avatar
                          }
                        }
                      }
                    }",
      Variables = new { id }
    };
    var res = await GQLClient.SendMutationAsync<StreamData>(request, cancellationToken).ConfigureAwait(false);
    return (await ExecuteGraphQLRequest<StreamData>(request, cancellationToken).ConfigureAwait(false)).stream;
  }

  /// <summary>
  /// Sends an email invite to join a stream and assigns them a collaborator role.
  /// </summary>
  /// <param name="streamCreateInput"></param>
  /// <returns></returns>
  public Task<bool> StreamInviteCreate(StreamInviteCreateInput streamCreateInput)
  {
    return StreamInviteCreate(CancellationToken.None, streamCreateInput);
  }

  /// <summary>
  /// Sends an email invite to join a stream and assigns them a collaborator role.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <param name="inviteCreateInput"></param>
  /// <returns></returns>
  public async Task<bool> StreamInviteCreate(
    CancellationToken cancellationToken,
    StreamInviteCreateInput inviteCreateInput
  )
  {
    if ((inviteCreateInput.email == null) & (inviteCreateInput.userId == null))
      throw new ArgumentException("You must provide either an email or a user id to create a stream invite");
    var request = new GraphQLRequest
    {
      Query =
        @"
          mutation streamInviteCreate($input: StreamInviteCreateInput!) {
            streamInviteCreate(input: $input)
          }",
      Variables = new { input = inviteCreateInput }
    };

    var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);
    return (bool)res["streamInviteCreate"];
  }

  /// <summary>
  /// Cancels an invite to join a stream.
  /// </summary>
  /// <param name="streamId">Id of the stream</param>
  /// <param name="inviteId">Id of the invite to cancel</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<bool> StreamInviteCancel(
    string streamId,
    string inviteId,
    CancellationToken cancellationToken = default
  )
  {
    var request = new GraphQLRequest
    {
      Query =
        @"
            mutation streamInviteCancel( $streamId: String!, $inviteId: String! ) {
              streamInviteCancel(streamId: $streamId, inviteId: $inviteId)
            }",
      Variables = new { streamId, inviteId }
    };

    var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);
    return (bool)res["streamInviteCancel"];
  }

  /// <summary>
  /// Checks if Speckle Server version is at least v2.6.4 meaning stream invites are supported.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns>true if invites are supported</returns>
  /// <exception cref="SpeckleException">if Speckle Server version is less than v2.6.4</exception>
  [Obsolete("We're not supporting 2.6.4 version any more", true)]
  public async Task<bool> _CheckStreamInvitesSupported(CancellationToken cancellationToken = default)
  {
    var version = ServerVersion ?? await GetServerVersion(cancellationToken).ConfigureAwait(false);
    if (version < new System.Version("2.6.4"))
      throw new SpeckleException("Stream invites are only supported as of Speckle Server v2.6.4.");
    return true;
  }

  /// <summary>
  /// Accept or decline a stream invite.
  /// </summary>
  /// <param name="streamId"></param>
  /// <param name="token"></param>
  /// <param name="accept"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <exception cref="SpeckleException"></exception>
  public async Task<bool> StreamInviteUse(
    string streamId,
    string token,
    bool accept = true,
    CancellationToken cancellationToken = default
  )
  {
    var request = new GraphQLRequest
    {
      Query =
        @"
            mutation streamInviteUse( $accept: Boolean!, $streamId: String!, $token: String! ) {
              streamInviteUse(accept: $accept, streamId: $streamId, token: $token)
            }",
      Variables = new
      {
        streamId,
        token,
        accept
      }
    };

    var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);
    return (bool)res["streamInviteUse"];
  }

  public async Task<List<PendingStreamCollaborator>> GetAllPendingInvites(CancellationToken cancellationToken = default)
  {
    var request = new GraphQLRequest
    {
      Query =
        @"
            query StreamInvites {
              streamInvites{
                id
                token
                inviteId
                streamId
                streamName
                title
                role
                invitedBy {
                  id
                  name
                  company
                  avatar
                }
              }
            }"
    };

    var res = await ExecuteGraphQLRequest<StreamInvitesResponse>(request, cancellationToken).ConfigureAwait(false);
    return res.streamInvites;
  }
}
