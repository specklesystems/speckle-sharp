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
  /// Gets a given model from a project.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <param name="projectId">Id of the project to get the model from</param>
  /// <param name="modelId">Id of the model</param>
  /// <returns></returns>
  public async Task<Branch> ModelGet(
    CancellationToken cancellationToken,
    string projectId,
    string modelId
  )
  {
    var request = new GraphQLRequest
    {
      Query =
        $@"query ProjectModel($projectId: String!, $modelId: String!) {{
                      project(id: $projectId) {{
                        model(id: $modelId){{
                          id,
                          name,
                          description
                        }}                       
                      }}
                    }}",
      Variables = new { projectId, modelId }
    };

    var res = await ExecuteGraphQLRequest<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(request, cancellationToken).ConfigureAwait(false);
    var branch = new Branch();
    branch.description = res["project"]["model"]["description"];
    branch.id = res["project"]["model"]["id"];
    branch.name = res["project"]["model"]["name"];
    return branch;
  }

  /// <summary>
  /// Gets a given model from a project.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <param name="projectId">Id of the project to get the model from</param>
  /// <param name="modelId">Id of the model</param>
  /// <returns></returns>
  public async Task<Branch> ModelGet(
    string projectId,
    string modelId
  )
  {
    return await ModelGet(CancellationToken.None, projectId, modelId);
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
