using System;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Api.GraphQL.Resources;

namespace Speckle.Core.Api;

public partial class Client
{
  /// <summary>
  /// Gets the comments on a Stream
  /// </summary>
  /// <param name="streamId">Id of the stream to get the comments from</param>
  /// <param name="limit">The number of comments to get</param>
  /// <param name="cursor">Time to filter the comments with</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <seealso cref="CommentResource.GetProjectComments"/>
  [Obsolete($"Use client.{nameof(CommentResource)}.{nameof(CommentResource.GetProjectComments)}", true)]
  public async Task<Comments> StreamGetComments(
    string streamId,
    int limit = 25,
    string? cursor = null,
    CancellationToken cancellationToken = default
  )
  {
    var request = new GraphQLRequest
    {
      Query =
        @"query Comments($streamId: String!, $cursor: String, $limit: Int!) {
                      comments(streamId: $streamId, cursor: $cursor, limit: $limit) {
                          totalCount
                          cursor
                          items {
                            id
                            authorId
                            archived
                            rawText
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
                                rawText
                                data
                                createdAt
                                updatedAt
                                viewedAt
                            }
                          }
                        }                                    
                      }
                    }",
      Variables = new
      {
        streamId,
        cursor,
        limit
      }
    };

    var res = await ExecuteGraphQLRequest<CommentsData>(request, cancellationToken).ConfigureAwait(false);
    return res.comments;
  }

  /// <summary>
  /// Gets the screenshot of a Comment
  /// </summary>
  /// <param name="id">Id of the comment</param>
  /// <param name="streamId">Id of the stream to get the comment from</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <seealso cref="CommentResource.GetProjectComments"/>
  [Obsolete($"Use client.{nameof(CommentResource)}.{nameof(CommentResource.GetProjectComments)}", true)]
  public async Task<string> StreamGetCommentScreenshot(
    string id,
    string streamId,
    CancellationToken cancellationToken = default
  )
  {
    var request = new GraphQLRequest
    {
      Query =
        @"query Comment($id: String!, $streamId: String!) {
                      comment(id: $id, streamId: $streamId) {
                            id
                            screenshot
                          }
                        }                                    
                    ",
      Variables = new { id, streamId }
    };

    var res = await ExecuteGraphQLRequest<CommentItemData>(request, cancellationToken).ConfigureAwait(false);
    return res.comment.screenshot;
  }
}
