#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;

namespace Speckle.Core.Api
{
  public partial class Client
  {
    /// <summary>
    /// Get branches from a given stream
    /// </summary>
    /// <param name="streamId">Id of the stream to get the branches from</param>
    /// <param name="branchesLimit">Max number of branches to retrieve</param>
    /// <param name="commitsLimit">Max number of commits to retrieve</param>
    /// <returns></returns>
    public Task<List<Branch>> StreamGetBranches(
      string streamId,
      int branchesLimit = 10,
      int commitsLimit = 10
    ) => StreamGetBranches(CancellationToken.None, streamId, branchesLimit, commitsLimit);

    /// <summary>
    /// Get branches from a given stream
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="streamId">Id of the stream to get the branches from</param>
    /// <param name="branchesLimit">Max number of branches to retrieve</param>
    /// <param name="commitsLimit">Max number of commits to retrieve</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<List<Branch>> StreamGetBranches(
      CancellationToken cancellationToken,
      string streamId,
      int branchesLimit = 10,
      int commitsLimit = 10
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
      var res = await ExecuteGraphQLRequest<StreamData>(request, cancellationToken);
      return res.stream.branches.items;
    }

    /// <summary>
    /// Creates a branch on a stream.
    /// </summary>
    /// <param name="branchInput"></param>
    /// <returns>The stream's id.</returns>
    public Task<string> BranchCreate(BranchCreateInput branchInput) =>
      BranchCreate(CancellationToken.None, branchInput);

    /// <summary>
    /// Creates a branch on a stream.
    /// </summary>
    /// <param name="branchInput"></param>
    /// <returns>The branch id.</returns>
    public async Task<string> BranchCreate(
      CancellationToken cancellationToken,
      BranchCreateInput branchInput
    )
    {
      var request = new GraphQLRequest
      {
        Query =
          @"mutation branchCreate($myBranch: BranchCreateInput!){ branchCreate(branch: $myBranch)}",
        Variables = new { myBranch = branchInput }
      };

      var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken);
      return (string)res["branchCreate"];
    }

    /// <summary>
    /// Gets a given branch from a stream.
    /// </summary>
    /// <param name="streamId">Id of the stream to get the branch from</param>
    /// <param name="branchName">Name of the branch to get</param>
    /// <returns></returns>
    public Task<Branch> BranchGet(string streamId, string branchName, int commitsLimit = 10) =>
      BranchGet(CancellationToken.None, streamId, branchName, commitsLimit);

    /// <summary>
    /// Gets a given branch from a stream.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="streamId">Id of the stream to get the branch from</param>
    /// <param name="branchName">Name of the branch to get</param>
    /// <returns></returns>
    public async Task<Branch> BranchGet(
      CancellationToken cancellationToken,
      string streamId,
      string branchName,
      int commitsLimit = 10
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

      var res = await ExecuteGraphQLRequest<StreamData>(request, cancellationToken);
      return res.stream.branch;
    }

    /// <summary>
    /// Updates a branch.
    /// </summary>
    /// <param name="branchInput"></param>
    /// <returns>The stream's id.</returns>
    public Task<bool> BranchUpdate(BranchUpdateInput branchInput) =>
      BranchUpdate(CancellationToken.None, branchInput);

    /// <summary>
    /// Updates a branch.
    /// </summary>
    /// <param name="branchInput"></param>
    /// <returns>The stream's id.</returns>
    public async Task<bool> BranchUpdate(
      CancellationToken cancellationToken,
      BranchUpdateInput branchInput
    )
    {
      var request = new GraphQLRequest
      {
        Query =
          @"mutation branchUpdate($myBranch: BranchUpdateInput!){ branchUpdate(branch: $myBranch)}",
        Variables = new { myBranch = branchInput }
      };

      var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken);
      return (bool)res["branchUpdate"];
    }

    /// <summary>
    /// Deletes a stream.
    /// </summary>
    /// <param name="branchInput"></param>
    /// <returns></returns>
    public Task<bool> BranchDelete(BranchDeleteInput branchInput) =>
      BranchDelete(CancellationToken.None, branchInput);

    /// <summary>
    /// Deletes a stream.
    /// </summary>
    /// <param name="branchInput"></param>
    /// <returns></returns>
    public async Task<bool> BranchDelete(
      CancellationToken cancellationToken,
      BranchDeleteInput branchInput
    )
    {
      var request = new GraphQLRequest
      {
        Query =
          @"mutation branchDelete($myBranch: BranchDeleteInput!){ branchDelete(branch: $myBranch)}",
        Variables = new { myBranch = branchInput }
      };

      var res = await ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken);
      return (bool)res["branchDelete"];
    }
  }
}
