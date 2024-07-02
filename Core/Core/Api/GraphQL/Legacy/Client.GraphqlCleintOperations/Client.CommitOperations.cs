using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Api.GraphQL.Resources;

namespace Speckle.Core.Api;

public partial class Client
{
  /// <summary>
  /// Gets a given commit from a stream.
  /// </summary>
  /// <param name="streamId">Id of the stream to get the commit from</param>
  /// <param name="commitId">Id of the commit to get</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <seealso cref="GraphQL.Resources.VersionResource.Get"/>
  [Obsolete($"Use client.{nameof(Version)}.{nameof(VersionResource.Get)}")]
  public async Task<Commit> CommitGet(string streamId, string commitId, CancellationToken cancellationToken = default)
  {
    var request = new GraphQLRequest
    {
      Query =
        @"query Stream($streamId: String!, $commitId: String!) {
                      stream(id: $streamId) {
                        commit(id: $commitId){
                          id,
                          message,
                          sourceApplication,
                          totalChildrenCount,
                          referencedObject,
                          branchName,
                          createdAt,
                          parents,
                          authorName
                        }                       
                      }
                    }",
      Variables = new { streamId, commitId }
    };

    var res = await ExecuteGraphQLRequest<StreamData>(request, cancellationToken).ConfigureAwait(false);
    return res.stream.commit;
  }

  /// <summary>
  /// Gets the latest commits from a stream
  /// </summary>
  /// <param name="streamId">Id of the stream to get the commits from</param>
  /// <param name="limit">Max number of commits to get</param>
  /// <param name="cancellationToken"></param>
  /// <returns>The requested commits</returns>
  /// <seealso cref="GraphQL.Resources.VersionResource.GetVersions"/>
  [Obsolete($"Use client.{nameof(Version)}.{nameof(VersionResource.GetVersions)}")]
  public async Task<List<Commit>> StreamGetCommits(
    string streamId,
    int limit = 10,
    CancellationToken cancellationToken = default
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

    var res = await ExecuteGraphQLRequest<StreamData>(request, cancellationToken).ConfigureAwait(false);
    return res.stream.commits.items;
  }

  /// <summary>
  /// Creates a commit on a branch.
  /// </summary>
  /// <param name="commitInput"></param>
  /// <returns>The commit id.</returns>
  /// <seealso cref="GraphQL.Resources.VersionResource.Create"/>
  [Obsolete($"Use client.{nameof(VersionResource)}.{nameof(VersionResource.Create)}")]
  public async Task<string> CommitCreate(CommitCreateInput commitInput, CancellationToken cancellationToken = default)
  {
    var request = new GraphQLRequest
    {
      Query = @"mutation commitCreate($myCommit: CommitCreateInput!){ commitCreate(commit: $myCommit)}",
      Variables = new { myCommit = commitInput }
    };

    var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);
    return (string)res["commitCreate"];
  }

  /// <summary>
  /// Updates a commit.
  /// </summary>
  /// <param name="commitInput"></param>
  /// <param name="cancellationToken"></param>
  /// <returns>The stream's id.</returns>
  /// <seealso cref="GraphQL.Resources.VersionResource.Update"/>
  [Obsolete($"Use client.{nameof(VersionResource)}.{nameof(VersionResource.Update)}")]
  public async Task<bool> CommitUpdate(CommitUpdateInput commitInput, CancellationToken cancellationToken = default)
  {
    var request = new GraphQLRequest
    {
      Query = @"mutation commitUpdate($myCommit: CommitUpdateInput!){ commitUpdate(commit: $myCommit)}",
      Variables = new { myCommit = commitInput }
    };

    var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);
    return (bool)res["commitUpdate"];
  }

  /// <summary>
  /// Deletes a commit.
  /// </summary>
  /// <param name="commitInput"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <seealso cref="GraphQL.Resources.VersionResource.Delete"/>
  [Obsolete($"Use client.{nameof(VersionResource)}.{nameof(VersionResource.Delete)}")]
  public async Task<bool> CommitDelete(CommitDeleteInput commitInput, CancellationToken cancellationToken = default)
  {
    var request = new GraphQLRequest
    {
      Query = @"mutation commitDelete($myCommit: CommitDeleteInput!){ commitDelete(commit: $myCommit)}",
      Variables = new { myCommit = commitInput }
    };

    var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);
    return (bool)res["commitDelete"];
  }

  /// <summary>
  /// Sends a commitReceived mutation, affirming a commit has been received.
  /// </summary>
  /// <remarks>Used for read receipts</remarks>
  /// <param name="commitReceivedInput"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <seealso cref="GraphQL.Resources.VersionResource.Received"/>
  [Obsolete($"Use client.{nameof(VersionResource)}.{nameof(VersionResource.Received)}")]
  public async Task<bool> CommitReceived(
    CommitReceivedInput commitReceivedInput,
    CancellationToken cancellationToken = default
  )
  {
    var request = new GraphQLRequest
    {
      Query = @"mutation($myInput:CommitReceivedInput!){ commitReceive(input:$myInput) }",
      Variables = new { myInput = commitReceivedInput }
    };

    var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);

    return (bool)res["commitReceive"];
  }
}
