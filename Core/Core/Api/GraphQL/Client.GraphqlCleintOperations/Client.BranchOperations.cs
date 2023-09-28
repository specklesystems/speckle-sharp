#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;

namespace Speckle.Core.Api;

public partial class Client
{
  /// <summary>
  /// Get branches from a given stream
  /// </summary>
  /// <param name="streamId">Id of the stream to get the branches from</param>
  /// <param name="branchesLimit">Max number of branches to retrieve</param>
  /// <param name="commitsLimit">Max number of commits to retrieve</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<List<Branch>> StreamGetBranches(
    string streamId,
    int branchesLimit = 10,
    int commitsLimit = 10,
    CancellationToken cancellationToken = default
  )
  {
    var request = new GraphQLRequest
    {
      Query =
        $@"query Stream ($streamId: String!) {{
                      stream(id: $streamId) {{
                        branches(limit: {branchesLimit}) {{
                          items {{
                            id
                            name
                            description
                            commits (limit: {commitsLimit}) {{
                              totalCount
                              cursor
                              items {{
                                id
                                referencedObject
                                sourceApplication
                                message
                                authorName
                                authorId
                                branchName
                                parents
                                createdAt
                              }}
                            }}
                          }}
                        }}                       
                      }}
                    }}",
      Variables = new { streamId }
    };
    var res = await ExecuteGraphQLRequest<StreamData>(request, cancellationToken).ConfigureAwait(false);
    return res.stream.branches.items;
  }

  /// <summary>
  /// Creates a branch on a stream.
  /// </summary>
  /// <param name="branchInput"></param>
  /// <param name="cancellationToken"></param>
  /// <returns>The branch id.</returns>
  public async Task<string> BranchCreate(BranchCreateInput branchInput, CancellationToken cancellationToken = default)
  {
    var request = new GraphQLRequest
    {
      Query = @"mutation branchCreate($myBranch: BranchCreateInput!){ branchCreate(branch: $myBranch)}",
      Variables = new { myBranch = branchInput }
    };

    var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);
    return (string)res["branchCreate"];
  }

  /// <summary>
  /// Gets a given branch from a stream.
  /// </summary>
  /// <param name="streamId">Id of the stream to get the branch from</param>
  /// <param name="branchName">Name of the branch to get</param>
  /// <param name="cancellationToken"></param>
  /// <returns>The requested branch</returns>
  public async Task<Branch> BranchGet(
    string streamId,
    string branchName,
    int commitsLimit = 10,
    CancellationToken cancellationToken = default
  )
  {
    var request = new GraphQLRequest
    {
      Query =
        $@"query Stream($streamId: String!, $branchName: String!) {{
                      stream(id: $streamId) {{
                        branch(name: $branchName){{
                          id,
                          name,
                          description,
                          commits (limit: {commitsLimit}) {{
                            totalCount,
                            cursor,
                            items {{
                              id,
                              referencedObject,
                              sourceApplication,
                              totalChildrenCount,
                              message,
                              authorName,
                              authorId,
                              branchName,
                              parents,
                              createdAt
                            }}
                          }}
                        }}                       
                      }}
                    }}",
      Variables = new { streamId, branchName }
    };

    var res = await ExecuteGraphQLRequest<StreamData>(request, cancellationToken).ConfigureAwait(false);
    return res.stream.branch;
  }

  /// <summary>
  /// Updates a branch.
  /// </summary>
  /// <param name="branchInput"></param>
  /// <returns>The stream's id.</returns>
  public async Task<bool> BranchUpdate(BranchUpdateInput branchInput, CancellationToken cancellationToken = default)
  {
    var request = new GraphQLRequest
    {
      Query = @"mutation branchUpdate($myBranch: BranchUpdateInput!){ branchUpdate(branch: $myBranch)}",
      Variables = new { myBranch = branchInput }
    };

    var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);
    return (bool)res["branchUpdate"];
  }

  /// <summary>
  /// Deletes a stream.
  /// </summary>
  /// <param name="branchInput"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<bool> BranchDelete(BranchDeleteInput branchInput, CancellationToken cancellationToken = default)
  {
    var request = new GraphQLRequest
    {
      Query = @"mutation branchDelete($myBranch: BranchDeleteInput!){ branchDelete(branch: $myBranch)}",
      Variables = new { myBranch = branchInput }
    };

    var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken).ConfigureAwait(false);
    return (bool)res["branchDelete"];
  }
}
