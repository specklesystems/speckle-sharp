#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;

namespace Speckle.Core.Api
{
  public partial class Client
  {
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
    public Task<List<ActivityItem>> StreamGetActivity(
      string streamId,
      DateTime? after = null,
      DateTime? before = null,
      DateTime? cursor = null,
      string actionType = "",
      int limit = 10
    ) =>
      StreamGetActivity(CancellationToken.None, streamId, after, before, cursor, actionType, limit);

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
    public async Task<List<ActivityItem>> StreamGetActivity(
      CancellationToken cancellationToken,
      string id,
      DateTime? after = null,
      DateTime? before = null,
      DateTime? cursor = null,
      string actionType = "",
      int limit = 25
    )
    {
      var request = new GraphQLRequest
      {
        Query =
          @"query Stream($id: String!, $before: DateTime,$after: DateTime, $cursor: DateTime, $activity: String, $limit: Int!) {
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
        Variables = new
        {
          id,
          limit,
          actionType,
          after,
          before,
          cursor
        }
      };

      var res = await ExecuteGraphQLRequest<StreamData>(request, cancellationToken);
      return res.stream.activity.items;
    }
  }
}
