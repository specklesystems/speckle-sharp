using System.Threading;
using System.Threading.Tasks;
using GraphQL;

namespace Speckle.Core.Api;

public partial class Client
{
  //TODO: API Gap
  /// <summary>
  /// Gets data about the requested Speckle object from a stream.
  /// </summary>
  /// <param name="streamId">Id of the stream to get the object from</param>
  /// <param name="objectId">Id of the object to get</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<SpeckleObject> ObjectGet(
    string streamId,
    string objectId,
    CancellationToken cancellationToken = default
  )
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
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<SpeckleObject> ObjectCountGet(
    string streamId,
    string objectId,
    CancellationToken cancellationToken = default
  )
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
