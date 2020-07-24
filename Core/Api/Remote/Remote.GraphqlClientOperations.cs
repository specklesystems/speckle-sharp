using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;
using Speckle.Core.Api.GqlModels;
using Speckle.Core.Models;

namespace Speckle.Core.Api
{
  public partial class Remote
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
                        username,
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
          throw new Exception(res.Errors[0].Message);

        return res.Data.user;
      }
      catch (Exception e)
      {
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
          throw new Exception(res.Errors[0].Message);

        return res.Data.stream;
      }
      catch (Exception e)
      {
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
                        username,
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
          throw new Exception(res.Errors[0].Message);

        return res.Data.user.streams.items;
      }
      catch (Exception e)
      {
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
          throw new Exception(res.Errors[0].Message);

        return (string)res.Data["streamCreate"];
      }
      catch (Exception e)
      {
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
          throw new Exception(res.Errors[0].Message);

        return (bool)res.Data["streamUpdate"];
      }
      catch (Exception e)
      {
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
        {
          throw new Exception(res.Errors[0].Message);
        }

        return (bool)res.Data["streamDelete"];
      }
      catch (Exception e)
      {
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
    public async Task<bool> StreamGrantPermission(string streamId, string userId, string role)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"mutation streamGrantPermission($streamId: String!, $userId: String!, $role: String!)
                    { streamGrantPermission(streamId:$streamId, userId:$userId, role:$role ) }",
          Variables = new
          {
            streamId,
            userId,
            role
          }
        };

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request);

        if (res.Errors != null)
          throw new Exception(res.Errors[0].Message);

        return (bool)res.Data["streamGrantPermission"];
      }
      catch (Exception e)
      {
        throw e;
      }
    }

    /// <summary>
    /// Revokes permissions of a user on a given stream.
    /// </summary>
    /// <param name="streamId"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<bool> StreamRevokePermission(string streamId, string userId)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"mutation streamRevokePermission($streamId: String!, $userId: String!)
                    { streamRevokePermission(streamId:$streamId, userId:$userId) }",
          Variables = new
          {
            streamId,
            userId,
          }
        };

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request);

        if (res.Errors != null)
          throw new Exception(res.Errors[0].Message);

        return (bool)res.Data["streamRevokePermission"];
      }
      catch (Exception e)
      {
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
          throw new Exception(res.Errors[0].Message);

        return (string)res.Data["branchCreate"];
      }
      catch (Exception e)
      {
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
          throw new Exception(res.Errors[0].Message);

        return (bool)res.Data["branchUpdate"];
      }
      catch (Exception e)
      {
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
        {
          throw new Exception(res.Errors[0].Message);
        }

        return (bool)res.Data["branchDelete"];
      }
      catch (Exception e)
      {
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
          throw new Exception(res.Errors[0].Message);

        return (string)res.Data["commitCreate"];
      }
      catch (Exception e)
      {
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
          throw new Exception(res.Errors[0].Message);

        return (bool)res.Data["commitUpdate"];
      }
      catch (Exception e)
      {
        throw e;
      }
    }

    /// <summary>
    /// Deletes a commit.
    /// </summary>
    /// <param name="commitInput"></param>
    /// <returns></returns>
    public async Task<bool> CommmitDelete(CommitDeleteInput commitInput)
    {
      try
      {
        var request = new GraphQLRequest
        {
          Query = @"mutation commitDelete($myCommit: CommitDeleteInput!){ commitDelete(commit: $myCommit)}",
          Variables = new
          {
            myBranch = commitInput
          }
        };

        var res = await GQLClient.SendMutationAsync<Dictionary<string, object>>(request);

        if (res.Errors != null)
        {
          throw new Exception(res.Errors[0].Message);
        }

        return (bool)res.Data["commitDelete"];
      }
      catch (Exception e)
      {
        throw e;
      }
    }
    #endregion

  }
}
