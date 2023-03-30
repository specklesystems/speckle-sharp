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
    /// Gets a given commit from a stream.
    /// </summary>
    /// <param name="streamId">Id of the stream to get the commit from</param>
    /// <param name="commitId">Id of the commit to get</param>
    /// <returns></returns>
    public Task<Commit> CommitGet(string streamId, string commitId) =>
      CommitGet(CancellationToken.None, streamId, commitId);

    /// <summary>
    /// Gets a given commit from a stream.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="streamId">Id of the stream to get the commit from</param>
    /// <param name="commitId">Id of the commit to get</param>
    /// <returns></returns>
    public async Task<Commit> CommitGet(
      CancellationToken cancellationToken,
      string streamId,
      string commitId
    )
    {
      var request = new GraphQLRequest
      {
        Query =
          $@"query Stream($streamId: String!, $commitId: String!) {{
                      stream(id: $streamId) {{
                        commit(id: $commitId){{
                          id,
                          message,
                          sourceApplication,
                          totalChildrenCount,
                          referencedObject,
                          branchName,
                          createdAt,
                          parents,
                          authorName
                        }}                       
                      }}
                    }}",
        Variables = new { streamId, commitId }
      };

      var res = await ExecuteGraphQLRequest<StreamData>(request, cancellationToken);
      return res.stream.commit;
    }

    /// <summary>
    /// Gets the latest commits from a stream
    /// </summary>
    /// <param name="streamId">Id of the stream to get the commits from</param>
    /// <param name="limit">Max number of commits to get</param>
    /// <returns></returns>
    public Task<List<Commit>> StreamGetCommits(string streamId, int limit = 10) =>
      StreamGetCommits(CancellationToken.None, streamId, limit);

    /// <summary>
    /// Gets the latest commits from a stream
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="streamId">Id of the stream to get the commits from</param>
    /// <param name="limit">Max number of commits to get</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<List<Commit>> StreamGetCommits(
      CancellationToken cancellationToken,
      string streamId,
      int limit = 10
    )
    {
      var request = new GraphQLRequest
      {
        Query =
          @"query Stream($streamId: String!, $limit: Int!) {
                      stream(id: $streamId) {
                        commits(limit: $limit) {
                          items {
                            id,
                            message,
                            branchName,
                            sourceApplication,
                            totalChildrenCount,
                            referencedObject,
                            createdAt,
                            parents,
                            authorName,
                            authorId,
                            authorAvatar
                          }
                        }                     
                      }
                    }",
        Variables = new { streamId, limit }
      };

      var res = await ExecuteGraphQLRequest<StreamData>(request, cancellationToken);
      return res.stream.commits.items;
    }

    /// <summary>
    /// Creates a commit on a branch.
    /// </summary>
    /// <param name="commitInput"></param>
    /// <returns>The commit id.</returns>
    public Task<string> CommitCreate(CommitCreateInput commitInput) =>
      CommitCreate(CancellationToken.None, commitInput);


    /// <inheritdoc cref="CommitCreate(CommitCreateInput)"/>
    /// <param name="cancellationToken"></param>
    public async Task<string> CommitCreate(
      CancellationToken cancellationToken,
      CommitCreateInput commitInput
    )
    {
      var request = new GraphQLRequest
      {
        Query =
          @"mutation commitCreate($myCommit: CommitCreateInput!){ commitCreate(commit: $myCommit)}",
        Variables = new { myCommit = commitInput }
      };

      var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken);
      return (string)res["commitCreate"];
    }

    /// <summary>
    /// Updates a commit.
    /// </summary>
    /// <param name="commitInput"></param>
    /// <returns>The stream's id.</returns>
    public Task<bool> CommitUpdate(CommitUpdateInput commitInput) =>
      CommitUpdate(CancellationToken.None, commitInput);

    /// <summary>
    /// Updates a commit.
    /// </summary>
    /// <param name="commitInput"></param>
    /// <returns>The stream's id.</returns>
    public async Task<bool> CommitUpdate(
      CancellationToken cancellationToken,
      CommitUpdateInput commitInput
    )
    {
      var request = new GraphQLRequest
      {
        Query =
          @"mutation commitUpdate($myCommit: CommitUpdateInput!){ commitUpdate(commit: $myCommit)}",
        Variables = new { myCommit = commitInput }
      };

      var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken);
      return (bool)res["commitUpdate"];
    }

    /// <summary>
    /// Deletes a commit.
    /// </summary>
    /// <param name="commitInput"></param>
    /// <returns></returns>
    public Task<bool> CommitDelete(CommitDeleteInput commitInput) =>
      CommitDelete(CancellationToken.None, commitInput);

    /// <summary>
    /// Deletes a commit.
    /// </summary>
    /// <param name="commitInput"></param>
    /// <returns></returns>
    public async Task<bool> CommitDelete(
      CancellationToken cancellationToken,
      CommitDeleteInput commitInput
    )
    {
      var request = new GraphQLRequest
      {
        Query =
          @"mutation commitDelete($myCommit: CommitDeleteInput!){ commitDelete(commit: $myCommit)}",
        Variables = new { myCommit = commitInput }
      };

      var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken);
      return (bool)res["commitDelete"];
    }

    /// <summary>
    /// Sends a commitReceived mutation, affirming a commit has been received.
    /// </summary>
    /// <remarks>Used for read receipts</remarks>
    /// <param name="commitReceivedInput"></param>
    /// <returns></returns>
    public Task<bool> CommitReceived(CommitReceivedInput commitReceivedInput) =>
      CommitReceived(CancellationToken.None, commitReceivedInput);

    /// <inheritdoc cref="CommitReceived(CommitReceivedInput)"/>
    /// <param name="cancellationToken"></param>
    public async Task<bool> CommitReceived(
      CancellationToken cancellationToken,
      CommitReceivedInput commitReceivedInput
    )
    {
      var request = new GraphQLRequest
      {
        Query = @"mutation($myInput:CommitReceivedInput!){ commitReceive(input:$myInput) }",
        Variables = new { myInput = commitReceivedInput }
      };

      var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken);

      return (bool)res["commitReceive"];
    }
  }
}
