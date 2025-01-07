using System;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Models.Responses;

namespace Speckle.Core.Api.GraphQL.Resources;

public sealed class CommentResource
{
  internal const string OBSOLETE_MESSAGE =
    "This function is longer compatible with server >=2.22, update nuget reference to Speckle.Sdk for comment mutations";
  private readonly ISpeckleGraphQLClient _client;

  internal CommentResource(ISpeckleGraphQLClient client)
  {
    _client = client;
  }

  /// <param name="projectId"></param>
  /// <param name="limit">Max number of comments to fetch</param>
  /// <param name="cursor">Optional cursor for pagination</param>
  /// <param name="filter">Optional filter</param>
  /// <param name="repliesLimit">Max number of comment replies to fetch</param>
  /// <param name="repliesCursor">Optional cursor for pagination</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<ResourceCollection<Comment>> GetProjectComments(
    string projectId,
    int limit = ServerLimits.DEFAULT_PAGINATION_REQUEST,
    string? cursor = null,
    ProjectCommentsFilter? filter = null,
    int repliesLimit = ServerLimits.DEFAULT_PAGINATION_REQUEST,
    string? repliesCursor = null,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
      query CommentThreads($projectId: String!, $cursor: String, $limit: Int!, $filter: ProjectCommentsFilter, $repliesLimit: Int, $repliesCursor: String) {
        project(id: $projectId) {
          commentThreads(cursor: $cursor, limit: $limit, filter: $filter) {
            cursor
            totalArchivedCount
            totalCount
            items {
              archived
              authorId
              createdAt
              hasParent
              id
              rawText
              replies(limit: $repliesLimit, cursor: $repliesCursor) {
                cursor
                items {
                  archived
                  authorId
                  createdAt
                  hasParent
                  id
                  rawText
                  updatedAt
                  viewedAt
                }
                totalCount
              }
              resources {
                resourceId
                resourceType
              }
              screenshot
              updatedAt
              viewedAt
              viewerResources {
                modelId
                objectId
                versionId
              }
              viewerState
              data
            }
          }
        }
      }
      """;

    GraphQLRequest request =
      new()
      {
        Query = QUERY,
        Variables = new
        {
          projectId,
          cursor,
          limit,
          filter,
          repliesLimit,
          repliesCursor,
        }
      };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectResponse>(request, cancellationToken)
      .ConfigureAwait(false);

    return response.project.commentThreads;
  }

  /// <param name="commentId"></param>
  /// <param name="archive"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  [Obsolete(OBSOLETE_MESSAGE)]
  public async Task<bool> Archive(string commentId, bool archive = true, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
      mutation Mutation($commentId: String!, $archive: Boolean!) {
        data:commentMutations {
           archive(commentId: $commentId, archived: $archive)
        }
      }
      """;
    GraphQLRequest request = new(QUERY, variables: new { commentId, archive });
    var res = await _client
      .ExecuteGraphQLRequest<RequiredResponse<CommentMutation>>(request, cancellationToken)
      .ConfigureAwait(false);
    return res.data.archive;
  }

  /// <param name="commentId"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  [Obsolete(OBSOLETE_MESSAGE)]
  public async Task<bool> MarkViewed(string commentId, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
      mutation Mutation($commentId: String!) {
        data:commentMutations {
          markViewed(commentId: $commentId)
        }
      }
      """;
    GraphQLRequest request = new(QUERY, variables: new { commentId });
    var res = await _client
      .ExecuteGraphQLRequest<RequiredResponse<CommentMutation>>(request, cancellationToken)
      .ConfigureAwait(false);
    return res.data.markViewed;
  }
}
