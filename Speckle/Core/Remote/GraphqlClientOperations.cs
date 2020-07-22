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
  }
}
