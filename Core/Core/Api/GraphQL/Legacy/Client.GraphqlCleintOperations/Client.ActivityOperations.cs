using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;

namespace Speckle.Core.Api;

public partial class Client
{
  //TODO: API gap
  /// <summary>
  /// Gets the activity of a stream
  /// </summary>
  /// <param name="id">Id of the stream to get the activity from</param>
  /// <param name="after">Only show activity after this DateTime</param>
  /// <param name="before">Only show activity before this DateTime</param>
  /// <param name="cursor">Time to filter the activity with</param>
  /// <param name="actionType">Time to filter the activity with</param>
  /// <param name="limit">Max number of activity items to get</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<List<ActivityItem>> StreamGetActivity(
    string id,
    DateTime? after = null,
    DateTime? before = null,
    DateTime? cursor = null,
    string actionType = "",
    int limit = ServerLimits.DEFAULT_PAGINATION_REQUEST,
    CancellationToken cancellationToken = default
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

    var res = await ExecuteGraphQLRequest<StreamData>(request, cancellationToken).ConfigureAwait(false);
    return res.stream.activity.items;
  }
}
