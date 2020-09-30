using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Sentry.Protocol;
using Speckle.Core.Logging;

namespace Speckle.Core.Api
{
  public partial class Client
  {
    /// <summary>
    /// Gets the current user
    /// </summary>
    /// <returns></returns>
    public async Task<User> UserGet()
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"query User {
                      user{
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

        var res = await GQLClient.SendMutationAsync<UserData>(request);

        if (res.Errors != null)
          Log.CaptureAndThrow(new GraphQLException("Could not get user"), res.Errors);

        return res.Data.user;
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    /// <summary>
    /// Searches for a user on the server
    /// </summary>
    /// <param name="query">String to search for. Must be at least 3 characters</param>
    /// <param name="limit">Max number of users to return</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<List<User>> UserSearch(string query, int limit = 10)
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
        var res = await GQLClient.SendMutationAsync<UserSearchData>(request);

        if ( res.Errors != null )
          Log.CaptureAndThrow(new GraphQLException("Could not search users"), res.Errors);

        return res.Data.userSearch.items;
      }
      catch ( Exception e )
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    #region streams

    /// <summary>
    /// Gets a stream by id, includes commits and branches
    /// </summary>
    /// <param name="id">Id of the stream to get</param>
    /// <param name="branchesLimit">Max number of branches to retrieve</param>
    /// <param name="commitsLimit">Max number of commits per branch to retrieve</param>
    /// <returns></returns>
    public async Task<Stream> StreamGet(string id, int branchesLimit = 10, int commitsLimit = 10)
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
                        createdAt
                        updatedAt
                        collaborators {{
                          id
                          name
                          role
                        }},
                        branches (limit: {branchesLimit}){{
                          totalCount,
                          cursor,
                          items {{
                          id,
                          name,
                          description,
                          commits (limit: {commitsLimit}) {{
                            totalCount,
                            cursor,
                            items {{
                              id,
                              referencedObject,
                              message,
                              authorName,
                              authorId,
                              createdAt
                            }}
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

        var res = await GQLClient.SendMutationAsync<StreamData>(request);

        if (res.Errors != null)
          Log.CaptureAndThrow(new GraphQLException("Could not get stream"), res.Errors);

        return res.Data.stream;
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }


    /// <summary>
    /// Gets all streams for the current user
    /// </summary>
    /// <param name="limit">Max number of streams to return</param>
    /// <returns></returns>
    public async Task<List<Stream>> StreamsGet(int limit = 10)
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
                            createdAt,
                            updatedAt,
                            collaborators {{
                              id,
                              name,
                              role
                            }}
                          }}
                        }}
                      }}
                    }}"
        };

        var res = await GQLClient.SendMutationAsync<UserData>(request);

        if (res.Errors != null)
          Log.CaptureAndThrow(new GraphQLException("Could not get streams"), res.Errors);

        return res.Data.user.streams.items;
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    /// <summary>
    /// Searches the user's streams by name, description, and ID
    /// </summary>
    /// <param name="query">String query to search for</param>
    /// <param name="limit">Max number of streams to return</param>
    /// <returns></returns>
    public async Task<List<Stream>> StreamSearch(string query, int limit = 10)
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
                          createdAt,
                          updatedAt,
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

        var res = await GQLClient.SendMutationAsync<StreamsData>(request);

        if ( res.Errors != null )
          Log.CaptureAndThrow(new GraphQLException("Could not search streams"), res.Errors);

        return res.Data.streams.items;
      }
      catch ( Exception e )
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    /// <summary>
    /// Creates a stream.
    /// </summary>
    /// <param name="streamInput"></param>
    /// <returns>The stream's id.</returns>
    public async Task<string> StreamCreate(StreamCreateInput streamInput)
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

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request);

        if (res.Errors != null)
          Log.CaptureAndThrow(new GraphQLException("Could not create stream"), res.Errors);

        return (string)res.Data["streamCreate"];
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    /// <summary>
    /// Updates a stream.
    /// </summary>
    /// <param name="streamInput">Note: the id field needs to be a valid stream id.</param>
    /// <returns>The stream's id.</returns>
    public async Task<bool> StreamUpdate(StreamUpdateInput streamInput)
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

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request);

        if (res.Errors != null)
          Log.CaptureAndThrow(new GraphQLException("Could not update stream"), res.Errors);

        return (bool)res.Data["streamUpdate"];
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    /// <summary>
    /// Deletes a stream.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<bool> StreamDelete(string id)
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

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request);

        if (res.Errors != null)
          Log.CaptureAndThrow(new GraphQLException("Could not delete stream"), res.Errors);


        return (bool)res.Data["streamDelete"];
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    /// <summary>
    /// Grants permissions to a user on a given stream.
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="userId"></param>
    /// <param name="role"></param>
    /// <returns></returns>
    public async Task<bool> StreamGrantPermission(StreamGrantPermissionInput permissionInput)
    {
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

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request);

        if (res.Errors != null)
          Log.CaptureAndThrow(new GraphQLException("Could not grant permission"), res.Errors);

        return (bool)res.Data["streamGrantPermission"];
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    /// <summary>
    /// Revokes permissions of a user on a given stream.
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<bool> StreamRevokePermission(StreamRevokePermissionInput permissionInput)
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

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request);

        if (res.Errors != null)
          Log.CaptureAndThrow(new GraphQLException("Could not revoke permission"), res.Errors);

        return (bool)res.Data["streamRevokePermission"];
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    #endregion

    #region branches
    /// <summary>
    /// Creates a branch on a stream.
    /// </summary>
    /// <param name="branchInput"></param>
    /// <returns>The stream's id.</returns>
    public async Task<string> BranchCreate(BranchCreateInput branchInput)
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

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request);

        if (res.Errors != null)
          Log.CaptureAndThrow(new GraphQLException("Could not create branch"), res.Errors);

        return (string)res.Data["branchCreate"];
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    /// <summary>
    /// Updates a branch.
    /// </summary>
    /// <param name="branchInput"></param>
    /// <returns>The stream's id.</returns>
    public async Task<bool> BranchUpdate(BranchUpdateInput branchInput)
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

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request);

        if (res.Errors != null)
          Log.CaptureAndThrow(new GraphQLException("Could not update branch"), res.Errors);

        return (bool)res.Data["branchUpdate"];
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    /// <summary>
    /// Deletes a stream.
    /// </summary>
    /// <param name="branchInput"></param>
    /// <returns></returns>
    public async Task<bool> BranchDelete(BranchDeleteInput branchInput)
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

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request);

        if (res.Errors != null)
          Log.CaptureAndThrow(new GraphQLException("Could not delete branch"), res.Errors);

        return (bool)res.Data["branchDelete"];
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }
    #endregion

    #region commits
    /// <summary>
    /// Creates a commit on a branch.
    /// </summary>
    /// <param name="commitInput"></param>
    /// <returns>The commit id.</returns>
    public async Task<string> CommitCreate(CommitCreateInput commitInput)
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

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request);

        if (res.Errors != null)
          Log.CaptureAndThrow(new GraphQLException("Could not create commit"), res.Errors);

        return (string)res.Data["commitCreate"];
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    /// <summary>
    /// Updates a commit.
    /// </summary>
    /// <param name="commitInput"></param>
    /// <returns>The stream's id.</returns>
    public async Task<bool> CommitUpdate(CommitUpdateInput commitInput)
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

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request);

        if (res.Errors != null)
          Log.CaptureAndThrow(new GraphQLException("Could not update commit"), res.Errors);

        return (bool)res.Data["commitUpdate"];
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }

    /// <summary>
    /// Deletes a commit.
    /// </summary>
    /// <param name="commitInput"></param>
    /// <returns></returns>
    public async Task<bool> CommitDelete(CommitDeleteInput commitInput)
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

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request);

        if (res.Errors != null)
          Log.CaptureAndThrow(new GraphQLException("Could not delete commit"), res.Errors);

        return (bool)res.Data["commitDelete"];
      }
      catch (Exception e)
      {
        Log.CaptureException(e);
        throw e;
      }
    }
    #endregion

  }
}
