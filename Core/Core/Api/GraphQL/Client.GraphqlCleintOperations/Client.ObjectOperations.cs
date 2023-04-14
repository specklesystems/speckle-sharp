#nullable enable

using System.Threading;
using System.Threading.Tasks;
using GraphQL;

namespace Speckle.Core.Api;

public partial class Client
{
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
    var request = new GraphQLRequest
    {
      Query =
        @"query Stream($streamId: String!, $objectId: String!) {
                      stream(id: $streamId) {
                        object(id: $objectId){
                          id
                          applicationId
                          createdAt
                          totalChildrenCount
                        }                       
                      }
                    }",
      Variables = new { streamId, objectId }
    };

    var res = await ExecuteGraphQLRequest<StreamData>(request, cancellationToken).ConfigureAwait(false);
    return res.stream.@object;
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
    var request = new GraphQLRequest
    {
      Query =
        @"query Stream($streamId: String!, $objectId: String!) {
                      stream(id: $streamId) {
                        object(id: $objectId){
                          totalChildrenCount
                        }                       
                      }
                    }",
      Variables = new { streamId, objectId }
    };

    var res = await ExecuteGraphQLRequest<StreamData>(request, cancellationToken).ConfigureAwait(false);
    return res.stream.@object;
  }
}
