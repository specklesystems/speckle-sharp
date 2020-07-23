using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Newtonsoft.Json;
using Speckle.Core.GqlModels;

namespace Speckle.Core
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

    /// <summary>
    /// Gets all streams for the current user
    /// </summary>
    /// <returns></returns>
    public async Task<List<Stream>> StreamsGet()
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
                        streams(limit:20) {
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
                      }
                    }"
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
  }
}
