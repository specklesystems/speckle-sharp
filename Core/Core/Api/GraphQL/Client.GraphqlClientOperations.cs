﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Logging;
using Version = System.Version;

namespace Speckle.Core.Api
{
  public partial class Client
  {

    /// <summary>
    /// Gets the current user.
    /// </summary>
    /// <param name="id">If provided, retrieves th user with this user Id</param>
    /// <returns></returns>
    public Task<User> UserGet(string id = "")
    {
      return UserGet(CancellationToken.None, id);
    }

    /// <summary>
    /// Gets the current user.
    /// </summary>
    /// <param name="id">If provided, retrieves th user with this user Id</param>
    /// <returns></returns>
    public async Task<User> UserGet(CancellationToken cancellationToken, string id = "")
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"query User($id: String) {
                      user(id: $id){
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
        ,
          Variables = new
          {
            id
          }
        };

        var res = await GQLClient.SendMutationAsync<UserData>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null && res.Errors.Any())
          throw new SpeckleException(res.Errors[0].Message, res.Errors);

        return res.Data.user;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }


    /// <summary>
    /// Searches for a user on the server.
    /// </summary>
    /// <param name="query">String to search for. Must be at least 3 characters</param>
    /// <param name="limit">Max number of users to return</param>
    /// <returns></returns>
    public Task<List<User>> UserSearch(string query, int limit = 10)
    {
      return UserSearch(CancellationToken.None, query: query, limit: limit);
    }

    /// <summary>
    /// Searches for a user on the server.
    /// </summary>
    /// <param name="query">String to search for. Must be at least 3 characters</param>
    /// <param name="limit">Max number of users to return</param>
    /// <returns></returns>
    public async Task<List<User>> UserSearch(CancellationToken cancellationToken, string query, int limit = 10)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"query UserSearch($query: String!, $limit: Int!) {
                      userSearch(query: $query, limit: $limit) {
                        cursor,
                        items {
                          id
                          name
                          bio
                          company
                          avatar
                          verified
                        }
                      }
                    }",
          Variables = new
          {
            query,
            limit
          }
        };
        var res = await GQLClient.SendMutationAsync<UserSearchData>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null && res.Errors.Any())
          throw new SpeckleException(res.Errors[0].Message, res.Errors);

        return res.Data.userSearch.items;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    /// <summary>
    /// Gets the version of the current server. Useful for guarding against unsupported api calls on newer or older servers.
    /// </summary>
    /// <param name="cancellationToken">[Optional] defaults to an empty cancellation token</param>
    /// <returns><see cref="Version"/> object excluding any strings (eg "2.7.2-alpha.6995" becomes "2.7.2.6995")</returns>
    /// <exception cref="SpeckleException"></exception>
    ///
    public async Task<System.Version> GetServerVersion(CancellationToken cancellationToken = default)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"query Server {
                      serverInfo {
                          version
                        }
                    }",
        };

        var res = await GQLClient.SendMutationAsync<ServerInfoResponse>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null && res.Errors.Any())
          throw new SpeckleException(res.Errors[0].Message, res.Errors);

        ServerVersion = new System.Version(Regex.Replace(res.Data.serverInfo.version, "[-a-zA-Z]+", ""));
        return ServerVersion;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    #region streams

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
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"query Stream($id: String!) {{
                      stream(id: $id) {{
                        id
                        name
                        description
                        isPublic
                        role
                        createdAt
                        updatedAt
                        commentCount
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
          Variables = new
          {
            id
          }
        };

        var res = await GQLClient.SendMutationAsync<StreamData>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null)
          throw new SpeckleException("Could not get stream", res.Errors);

        return res.Data.stream;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
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
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"query User {{
                      user{{
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

        var res = await GQLClient.SendMutationAsync<UserData>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null)
          throw new SpeckleException("Could not get streams", res.Errors);
        if (res.Data?.user == null)
        {
          throw new SpeckleException("User is not authenticated, or the credentials were not valid. Check the provided account is still valid, remove it from manager and add it again.");
        }
        return res.Data.user.streams.items;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
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
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"query User {{
                      user{{
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

        var res = await GQLClient.SendMutationAsync<UserData>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null)
          throw new SpeckleException("Could not get favorite streams", res.Errors);

        return res.Data.user.favoriteStreams.items;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
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
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"query Streams ($query: String!, $limit: Int!) {
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
          Variables = new
          {
            query,
            limit
          }
        };

        var res = await GQLClient.SendMutationAsync<StreamsData>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null)
          throw new SpeckleException("Could not search streams", res.Errors);

        return res.Data.streams.items;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
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
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"mutation streamCreate($myStream: StreamCreateInput!) { streamCreate(stream: $myStream) }",
          Variables = new
          {
            myStream = streamInput
          }
        };

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null)
          throw new SpeckleException("Could not create stream", res.Errors);

        return (string)res.Data["streamCreate"];
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
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
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"mutation streamUpdate($myStream: StreamUpdateInput!) { streamUpdate(stream:$myStream) }",
          Variables = new
          {
            myStream = streamInput
          }
        };

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null)
          throw new SpeckleException("Could not update stream", res.Errors);

        return (bool)res.Data["streamUpdate"];
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
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
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"mutation streamDelete($id: String!) { streamDelete(id:$id) }",
          Variables = new
          {
            id
          }
        };

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null)
          throw new SpeckleException("Could not delete stream", res.Errors);


        return (bool)res.Data["streamDelete"];
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    /// <summary>
    /// Grants permissions to a user on a given stream.
    /// </summary>
    /// <param name="permissionInput"></param>
    /// <returns></returns>
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
    public async Task<bool> StreamGrantPermission(CancellationToken cancellationToken, StreamPermissionInput permissionInput)
    {
      var version = await GetServerVersion(cancellationToken);
      if (version >= new System.Version("2.6.4"))
      {
        throw new SpeckleException(
          "Server mutation `StreamGrantPermission` is no longer supported as of Speckle Server v2.6.4." +
          "Please use the new `StreamUpdatePermission` method to change an existing user's permission or use the" +
          "StreamInviteCreate method to invite a user to a stream."
          );
      }
      try
      {
        var request = new GraphQLRequest
        {
          Query =
          @"
          mutation streamGrantPermission($permissionParams: StreamGrantPermissionInput!) {
            streamGrantPermission(permissionParams:$permissionParams)
          }",
          Variables = new
          {
            permissionParams = permissionInput
          }
        };

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request).ConfigureAwait(false);

        if (res.Errors != null)
          throw new SpeckleException("Could not grant permission", res.Errors);

        return (bool)res.Data["streamGrantPermission"];
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
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
    public async Task<bool> StreamRevokePermission(CancellationToken cancellationToken, StreamRevokePermissionInput permissionInput)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query =
          @"mutation streamRevokePermission($permissionParams: StreamRevokePermissionInput!) {
            streamRevokePermission(permissionParams: $permissionParams)
          }",
          Variables = new
          {
            permissionParams = permissionInput
          }
        };

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request).ConfigureAwait(false);

        if (res.Errors != null)
          throw new SpeckleException("Could not revoke permission", res.Errors);

        return (bool)res.Data["streamRevokePermission"];
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    /// <summary>
    /// Updates permissions for a user on a given stream.
    /// Valid for Speckle Server >=2.6.4
    /// </summary>
    /// <param name="updatePermissionInput">includes the streamId, the userId of the user to update, and the user's new role</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="SpeckleException"></exception>
    public async Task<bool> StreamUpdatePermission(StreamPermissionInput updatePermissionInput, CancellationToken cancellationToken = default)
    {
      await _CheckStreamInvitesSupported(cancellationToken);
      try
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

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request).ConfigureAwait(false);

        if (res.Errors != null)
          throw new SpeckleException("Could not update permission", res.Errors);

        return (bool)res.Data["streamUpdatePermission"];
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
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
    public async Task<bool> StreamInviteCreate(CancellationToken cancellationToken, StreamInviteCreateInput inviteCreateInput)
    {
      if (inviteCreateInput.email == null & inviteCreateInput.userId == null)
        throw new SpeckleException("You must provide either an email or a user id to create a stream invite");
      try
      {
        // not guarded by _CheckStreamInvitesSupported because this was here already so i'm guessing this particular
        // endpoint was still supported prior to 2.6.4 even though the rest of the flow was not
        var request = new GraphQLRequest
        {
          Query =
          @"
          mutation streamInviteCreate($input: StreamInviteCreateInput!) {
            streamInviteCreate(input: $input)
          }",
          Variables = new
          {
            input = inviteCreateInput
          }
        };

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request).ConfigureAwait(false);

        if (res.Errors != null)
          throw new SpeckleException("Could not create stream invite", res.Errors);

        return (bool)res.Data["streamInviteCreate"];
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    /// <summary>
    /// Checks if Speckle Server version is at least v2.6.4 meaning stream invites are supported.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>true if invites are supported</returns>
    /// <exception cref="SpeckleException">if Speckle Server version is less than v2.6.4</exception>
    public async Task<bool> _CheckStreamInvitesSupported(CancellationToken cancellationToken = default)
    {
      var version = ServerVersion ?? await GetServerVersion(cancellationToken);
      if (version < new System.Version("2.6.4"))
        throw new SpeckleException("Stream invites are only supported as of Speckle Server v2.6.4.");
      return true;
    }

    /// <summary>
    /// Accept or decline a stream invite.
    /// Requires Speckle Server version >= 2.6 .4
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="token"></param>
    /// <param name="accept"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="SpeckleException"></exception>
    public async Task<bool> StreamInviteUse(string streamId, string token, bool accept = true,
      CancellationToken cancellationToken = default)
    {
      await _CheckStreamInvitesSupported(cancellationToken);
      try
      {
        var request = new GraphQLRequest
        {
          Query =
            @"
            mutation streamInviteUse( $accept: Boolean!, $streamId: String!, $token: String! ) {
              streamInviteUse(accept: $accept, streamId: $streamId, token: $token)
            }",
          Variables = new { streamId, token, accept }
        };

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request).ConfigureAwait(false);

        if (res.Errors != null)
          throw new SpeckleException(
            $"Could not {(accept ? "accept" : "decline")} the invite for stream {streamId}",
            res.Errors);

        return (bool)res.Data["streamInviteUse"];
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    public async Task<List<PendingStreamCollaborator>> GetAllPendingInvites(CancellationToken cancellationToken = default)
    {
      await _CheckStreamInvitesSupported(cancellationToken);
      try
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
            }",
        };

        var res = await GQLClient.SendMutationAsync<StreamInvitesResponse>(request).ConfigureAwait(false);

        if (res.Errors != null)
          throw new SpeckleException(
            "Could not get pending stream invites for current user",
            res.Errors);

        return res.Data.streamInvites;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    #endregion

    #region branches

    /// <summary>
    /// Get branches from a given stream
    /// </summary>
    /// <param name="streamId">Id of the stream to get the branches from</param>
    /// <param name="branchesLimit">Max number of branches to retrieve</param>
    /// <param name="commitsLimit">Max number of commits to retrieve</param>
    /// <returns></returns>
    public Task<List<Branch>> StreamGetBranches(string streamId, int branchesLimit = 10, int commitsLimit = 10)
    {
      return StreamGetBranches(CancellationToken.None, streamId, branchesLimit, commitsLimit);
    }

    /// <summary>
    /// Get branches from a given stream
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="streamId">Id of the stream to get the branches from</param>
    /// <param name="branchesLimit">Max number of branches to retrieve</param>
    /// <param name="commitsLimit">Max number of commits to retrieve</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<List<Branch>> StreamGetBranches(CancellationToken cancellationToken, string streamId,
      int branchesLimit = 10, int commitsLimit = 10)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"query Stream ($streamId: String!) {{
                      stream(id: $streamId) {{
                        branches(limit: {branchesLimit}) {{
                          items {{
                            id
                            name
                            description
                            commits (limit: {commitsLimit}) {{
                              totalCount
                              cursor
                              items {{
                                id
                                referencedObject
                                sourceApplication
                                message
                                authorName
                                authorId
                                branchName
                                parents
                                createdAt
                              }}
                            }}
                          }}
                        }}                       
                      }}
                    }}",
          Variables = new { streamId }
        };

        var res = await GQLClient.SendMutationAsync<StreamData>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null && res.Errors.Any())
          throw new SpeckleException(res.Errors[0].Message, res.Errors);

        return res.Data.stream.branches.items;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    /// <summary>
    /// Creates a branch on a stream.
    /// </summary>
    /// <param name="branchInput"></param>
    /// <returns>The stream's id.</returns>
    public Task<string> BranchCreate(BranchCreateInput branchInput)
    {
      return BranchCreate(CancellationToken.None, branchInput);
    }

    /// <summary>
    /// Creates a branch on a stream.
    /// </summary>
    /// <param name="branchInput"></param>
    /// <returns>The branch id.</returns>
    public async Task<string> BranchCreate(CancellationToken cancellationToken, BranchCreateInput branchInput)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"mutation branchCreate($myBranch: BranchCreateInput!){ branchCreate(branch: $myBranch)}",
          Variables = new
          {
            myBranch = branchInput
          }
        };

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null)
          throw new SpeckleException("Could not create branch", res.Errors);

        return (string)res.Data["branchCreate"];
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    /// <summary>
    /// Gets a given branch from a stream.
    /// </summary>
    /// <param name="streamId">Id of the stream to get the branch from</param>
    /// <param name="branchName">Name of the branch to get</param>
    /// <returns></returns>
    public Task<Branch> BranchGet(string streamId, string branchName, int commitsLimit = 10)
    {
      return BranchGet(CancellationToken.None, streamId, branchName, commitsLimit);
    }

    /// <summary>
    /// Gets a given branch from a stream.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="streamId">Id of the stream to get the branch from</param>
    /// <param name="branchName">Name of the branch to get</param>
    /// <returns></returns>
    public async Task<Branch> BranchGet(CancellationToken cancellationToken, string streamId, string branchName, int commitsLimit = 10)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"query Stream($streamId: String!, $branchName: String!) {{
                      stream(id: $streamId) {{
                        branch(name: $branchName){{
                          id,
                          name,
                          description,
                          commits (limit: {commitsLimit}) {{
                            totalCount,
                            cursor,
                            items {{
                              id,
                              referencedObject,
                              sourceApplication,
                              totalChildrenCount,
                              message,
                              authorName,
                              authorId,
                              branchName,
                              parents,
                              createdAt
                            }}
                          }}
                        }}                       
                      }}
                    }}",
          Variables = new
          {
            streamId,
            branchName
          }
        };

        var res = await GQLClient.SendMutationAsync<StreamData>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null && res.Errors.Any())
          throw new SpeckleException(res.Errors[0].Message, res.Errors);

        return res.Data.stream.branch;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    /// <summary>
    /// Updates a branch.
    /// </summary>
    /// <param name="branchInput"></param>
    /// <returns>The stream's id.</returns>
    public Task<bool> BranchUpdate(BranchUpdateInput branchInput)
    {
      return BranchUpdate(CancellationToken.None, branchInput);
    }

    /// <summary>
    /// Updates a branch.
    /// </summary>
    /// <param name="branchInput"></param>
    /// <returns>The stream's id.</returns>
    public async Task<bool> BranchUpdate(CancellationToken cancellationToken, BranchUpdateInput branchInput)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"mutation branchUpdate($myBranch: BranchUpdateInput!){ branchUpdate(branch: $myBranch)}",
          Variables = new
          {
            myBranch = branchInput
          }
        };

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null)
          throw new SpeckleException("Could not update branch", res.Errors);

        return (bool)res.Data["branchUpdate"];
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    /// <summary>
    /// Deletes a stream.
    /// </summary>
    /// <param name="branchInput"></param>
    /// <returns></returns>
    public Task<bool> BranchDelete(BranchDeleteInput branchInput)
    {
      return BranchDelete(CancellationToken.None, branchInput);
    }

    /// <summary>
    /// Deletes a stream.
    /// </summary>
    /// <param name="branchInput"></param>
    /// <returns></returns>
    public async Task<bool> BranchDelete(CancellationToken cancellationToken, BranchDeleteInput branchInput)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"mutation branchDelete($myBranch: BranchDeleteInput!){ branchDelete(branch: $myBranch)}",
          Variables = new
          {
            myBranch = branchInput
          }
        };

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null)
          throw new SpeckleException("Could not delete branch", res.Errors);

        return (bool)res.Data["branchDelete"];
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }
    #endregion

    #region commits

    /// <summary>
    /// Gets a given commit from a stream.
    /// </summary>
    /// <param name="streamId">Id of the stream to get the commit from</param>
    /// <param name="commitId">Id of the commit to get</param>
    /// <returns></returns>
    public Task<Commit> CommitGet(string streamId, string commitId)
    {
      return CommitGet(CancellationToken.None, streamId, commitId);
    }

    /// <summary>
    /// Gets a given commit from a stream.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="streamId">Id of the stream to get the commit from</param>
    /// <param name="commitId">Id of the commit to get</param>
    /// <returns></returns>
    public async Task<Commit> CommitGet(CancellationToken cancellationToken, string streamId, string commitId)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"query Stream($streamId: String!, $commitId: String!) {{
                      stream(id: $streamId) {{
                        commit(id: $commitId){{
                          id,
                          message,
                          sourceApplication,
                          totalChildrenCount,
                          referencedObject,
                          branchName,
                          createdAt,
                          parents,
                          authorName
                        }}                       
                      }}
                    }}",
          Variables = new
          {
            streamId,
            commitId
          }
        };

        var res = await GQLClient.SendMutationAsync<StreamData>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null && res.Errors.Any())
          throw new SpeckleException(res.Errors[0].Message, res.Errors);

        return res.Data.stream.commit;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    /// <summary>
    /// Gets the latest commits from a stream
    /// </summary>
    /// <param name="streamId">Id of the stream to get the commits from</param>
    /// <param name="limit">Max number of commits to get</param>
    /// <returns></returns>
    public Task<List<Commit>> StreamGetCommits(string streamId, int limit = 10)
    {
      return StreamGetCommits(CancellationToken.None, streamId, limit);
    }

    /// <summary>
    /// Gets the latest commits from a stream
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="streamId">Id of the stream to get the commits from</param>
    /// <param name="limit">Max number of commits to get</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<List<Commit>> StreamGetCommits(CancellationToken cancellationToken, string streamId, int limit = 10)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"query Stream($streamId: String!, $limit: Int!) {
                      stream(id: $streamId) {
                        commits(limit: $limit) {
                          items {
                            id,
                            message,
                            branchName,
                            sourceApplication,
                            totalChildrenCount,
                            referencedObject,
                            createdAt,
                            parents,
                            authorName,
                            authorId,
                            authorAvatar
                          }
                        }                     
                      }
                    }",
          Variables = new { streamId, limit }
        };

        var res = await GQLClient.SendMutationAsync<StreamData>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null && res.Errors.Any())
          throw new SpeckleException(res.Errors[0].Message, res.Errors);

        return res.Data.stream.commits.items;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    /// <summary>
    /// Creates a commit on a branch.
    /// </summary>
    /// <param name="commitInput"></param>
    /// <returns>The commit id.</returns>
    public Task<string> CommitCreate(CommitCreateInput commitInput)
    {
      return CommitCreate(CancellationToken.None, commitInput);
    }

    /// <summary>
    /// Creates a commit on a branch.
    /// </summary>
    /// <param name="commitInput"></param>
    /// <returns>The commit id.</returns>
    public async Task<string> CommitCreate(CancellationToken cancellationToken, CommitCreateInput commitInput)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"mutation commitCreate($myCommit: CommitCreateInput!){ commitCreate(commit: $myCommit)}",
          Variables = new
          {
            myCommit = commitInput
          }
        };

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null && res.Errors.Any())
          throw new SpeckleException(res.Errors[0].Message, res.Errors);

        return (string)res.Data["commitCreate"];
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    /// <summary>
    /// Updates a commit.
    /// </summary>
    /// <param name="commitInput"></param>
    /// <returns>The stream's id.</returns>
    public Task<bool> CommitUpdate(CommitUpdateInput commitInput)
    {
      return CommitUpdate(CancellationToken.None, commitInput);
    }

    /// <summary>
    /// Updates a commit.
    /// </summary>
    /// <param name="commitInput"></param>
    /// <returns>The stream's id.</returns>
    public async Task<bool> CommitUpdate(CancellationToken cancellationToken, CommitUpdateInput commitInput)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"mutation commitUpdate($myCommit: CommitUpdateInput!){ commitUpdate(commit: $myCommit)}",
          Variables = new
          {
            myCommit = commitInput
          }
        };

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null && res.Errors.Any())
          throw new SpeckleException(res.Errors[0].Message, res.Errors);

        return (bool)res.Data["commitUpdate"];
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    /// <summary>
    /// Deletes a commit.
    /// </summary>
    /// <param name="commitInput"></param>
    /// <returns></returns>
    public Task<bool> CommitDelete(CommitDeleteInput commitInput)
    {
      return CommitDelete(CancellationToken.None, commitInput);
    }

    /// <summary>
    /// Deletes a commit.
    /// </summary>
    /// <param name="commitInput"></param>
    /// <returns></returns>
    public async Task<bool> CommitDelete(CancellationToken cancellationToken, CommitDeleteInput commitInput)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"mutation commitDelete($myCommit: CommitDeleteInput!){ commitDelete(commit: $myCommit)}",
          Variables = new
          {
            myCommit = commitInput
          }
        };

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null && res.Errors.Any())
          throw new SpeckleException(res.Errors[0].Message, res.Errors);

        return (bool)res.Data["commitDelete"];
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }


    public Task<bool> CommitReceived(CommitReceivedInput commitReceivedInput)
    {
      return CommitReceived(CancellationToken.None, commitReceivedInput);
    }

    public async Task<bool> CommitReceived(CancellationToken cancellationToken, CommitReceivedInput commitReceivedInput)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"mutation($myInput:CommitReceivedInput!){ commitReceive(input:$myInput) }",
          Variables = new
          {
            myInput = commitReceivedInput
          }
        };

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null && res.Errors.Any())
          throw new SpeckleException(res.Errors[0].Message, res.Errors);

        return (bool)res.Data["commitReceive"];
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    #endregion

    #region activity

    /// <summary>
    /// Gets the activity of a stream
    /// </summary>
    /// <param name="streamId">Id of the stream to get the activity from</param>
    /// <param name="after">Only show activity after this DateTime</param>
    /// <param name="before">Only show activity before this DateTime</param>
    /// <param name="cursor">Time to filter the activity with</param>
    /// <param name="actionType">Time to filter the activity with</param>
    /// <param name="limit">Max number of activity items to get</param>
    /// <returns></returns>
    public Task<List<ActivityItem>> StreamGetActivity(string streamId, DateTime? after = null, DateTime? before = null, DateTime? cursor = null, string actionType = "", int limit = 10)
    {
      return StreamGetActivity(CancellationToken.None, streamId, after, before, cursor, actionType, limit);
    }

    /// <summary>
    /// Gets the activity of a stream
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="id">Id of the stream to get the activity from</param>
    /// <param name="after">Only show activity after this DateTime</param>
    /// <param name="before">Only show activity before this DateTime</param>
    /// <param name="cursor">Time to filter the activity with</param>
    /// <param name="actionType">Time to filter the activity with</param>
    /// <param name="limit">Max number of commits to get</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<List<ActivityItem>> StreamGetActivity(CancellationToken cancellationToken, string id, DateTime? after = null, DateTime? before = null, DateTime? cursor = null, string actionType = "", int limit = 25)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"query Stream($id: String!, $before: DateTime,$after: DateTime, $cursor: DateTime, $activity: String, $limit: Int!) {
                      stream(id: $id) {
                        activity (actionType: $activity, after: $after, before: $before, cursor: $cursor, limit: $limit) {
                          totalCount
                          cursor
                          items {
                            actionType
                            userId
                            streamId
                            resourceId
                            resourceType
                            time
                            info
                            message
                          }
                        }                
                      }
                    }",
          Variables = new { id, limit, actionType, after, before, cursor }
        };

        var res = await GQLClient.SendMutationAsync<StreamData>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null && res.Errors.Any())
          throw new SpeckleException(res.Errors[0].Message, res.Errors);

        return res.Data.stream.activity.items;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }


    #endregion

    #region comments

    /// <summary>
    /// Gets the comments on a Stream
    /// </summary>
    /// <param name="streamId">Id of the stream to get the comments from</param>
    /// <param name="limit">The number of comments to get</param>
    /// <param name="cursor">Time to filter the comments with</param>
    /// <returns></returns>
    public Task<Comments> StreamGetComments(string streamId, int limit = 25, string cursor = null)
    {
      return StreamGetComments(CancellationToken.None, streamId, limit, cursor);
    }

    /// <summary>
    ///  Gets the comments on a Stream
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="streamId">Id of the stream to get the comments from</param>
    /// <param name="limit">The number of comments to get</param>
    /// <param name="cursor">Time to filter the comments with</param>
    /// <returns></returns>
    /// <exception cref="SpeckleException"></exception>
    public async Task<Comments> StreamGetComments(CancellationToken cancellationToken, string streamId, int limit = 25, string cursor = null)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"query Comments($streamId: String!, $cursor: String, $limit: Int!) {
                      comments(streamId: $streamId, cursor: $cursor, limit: $limit) {
                          totalCount
                          cursor
                          items {
                            id
                            authorId
                            archived
                            text {
                              doc
                            }
                            data
                            createdAt
                            updatedAt
                            viewedAt
                            reactions
                            resources {
                              resourceId
                              resourceType
                            }
                            replies {
                              totalCount
                              cursor
                              items {
                                id
                                authorId
                                archived
                                text {
                                  doc
                                }
                                data
                                createdAt
                                updatedAt
                                viewedAt
                            }
                          }
                        }                                    
                      }
                    }",
          Variables = new { streamId, cursor, limit }
        };

        var res = await GQLClient.SendMutationAsync<CommentsData>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null && res.Errors.Any())
          throw new SpeckleException(res.Errors[0].Message, res.Errors);

        return res.Data.comments;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    /// <summary>
    ///  Gets the screenshot of a Comment
    /// </summary>
    /// <param name="id">Id of the comment</param>
    /// <param name="streamId">Id of the stream to get the comment from</param>
    /// <returns></returns>
    public Task<string> StreamGetCommentScreenshot(string id, string streamId)
    {
      return StreamGetCommentScreenshot(CancellationToken.None, id, streamId);
    }

    /// <summary>
    ///  Gets the screenshot of a Comment
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="id">Id of the comment</param>
    /// <param name="streamId">Id of the stream to get the comment from</param>
    /// <returns></returns>
    /// <exception cref="SpeckleException"></exception>
    public async Task<string> StreamGetCommentScreenshot(CancellationToken cancellationToken, string id, string streamId)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"query Comment($id: String!, $streamId: String!) {
                      comment(id: $id, streamId: $streamId) {
                            id
                            screenshot
                          }
                        }                                    
                    ",
          Variables = new { id, streamId }
        };

        var res = await GQLClient.SendMutationAsync<CommentItemData>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null && res.Errors.Any())
          throw new SpeckleException(res.Errors[0].Message, res.Errors);

        return res.Data.comment.screenshot;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }
    #endregion

    #region objects

    /// <summary>
    /// Gets a given object from a stream.
    /// </summary>
    /// <param name="streamId">Id of the stream to get the object from</param> 
    /// <param name="objectId">Id of the object to get</param>
    /// <returns></returns>
    public Task<SpeckleObject> ObjectGet(string streamId, string objectId)
    {
      return ObjectGet(CancellationToken.None, streamId, objectId);
    }

    /// <summary>
    /// Gets a given object from a stream.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="streamId">Id of the stream to get the object from</param>
    /// <param name="objectId">Id of the object to get</param>
    /// <returns></returns>
    public async Task<SpeckleObject> ObjectGet(CancellationToken cancellationToken, string streamId, string objectId)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"query Stream($streamId: String!, $objectId: String!) {{
                      stream(id: $streamId) {{
                        object(id: $objectId){{
                          id
                          applicationId
                          createdAt
                          totalChildrenCount
                        }}                       
                      }}
                    }}",
          Variables = new { streamId, objectId }
        };

        var res = await GQLClient.SendQueryAsync<StreamData>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null && res.Errors.Any())
          throw new SpeckleException(res.Errors[0].Message, res.Errors);

        return res.Data.stream.@object;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    /// <summary>
    /// Gets a given object from a stream.
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="objectId"></param>
    /// <returns></returns>
    public Task<SpeckleObject> ObjectCountGet(string streamId, string objectId)
    {
      return ObjectCountGet(CancellationToken.None, streamId, objectId);
    }

    /// <summary>
    /// Gets a given object from a stream.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="streamId"></param>
    /// <param name="objectId"></param>
    /// <returns></returns>
    public async Task<SpeckleObject> ObjectCountGet(CancellationToken cancellationToken, string streamId, string objectId)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = $@"query Stream($streamId: String!, $objectId: String!) {{
                      stream(id: $streamId) {{
                        object(id: $objectId){{
                          totalChildrenCount
                        }}                       
                      }}
                    }}",
          Variables = new { streamId, objectId }
        };

        var res = await GQLClient.SendQueryAsync<StreamData>(request, cancellationToken).ConfigureAwait(false);

        if (res.Errors != null && res.Errors.Any())
          throw new SpeckleException(res.Errors[0].Message, res.Errors);

        return res.Data.stream.@object;
      }
      catch (Exception e)
      {
        throw new SpeckleException(e.Message, e);
      }
    }

    #endregion
  }
}
