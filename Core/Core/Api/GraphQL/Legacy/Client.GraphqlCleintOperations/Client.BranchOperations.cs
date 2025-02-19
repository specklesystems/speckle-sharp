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
  /// Get branches from a given stream, first with a max of 500 and then with a max of 100.
  /// This ensures that if the server API is limiting to 100 branches, that any failure will try again at the lower value.
  /// </summary>
  /// <param name="streamId">Id of the stream to get the branches from</param>
  /// <param name="commitsLimit">Max number of commits to retrieve</param>
  /// <returns></returns>
  /// <seealso cref="GraphQL.Resources.ProjectResource.GetWithModels"/>
  /// <seealso cref="GraphQL.Resources.ModelResource.GetModels"/>
  [Obsolete($"Use client.{nameof(Model)}.{nameof(ModelResource.GetModels)}")]
  public async Task<List<Branch>> StreamGetBranchesWithLimitRetry(string streamId, int commitsLimit = 10)
  {
    List<Branch> branches;
    try
    {
      branches = await StreamGetBranches(streamId, ServerLimits.BRANCH_GET_LIMIT, commitsLimit).ConfigureAwait(true);
    }
    catch (SpeckleGraphQLException<StreamData>)
    {
      branches = await StreamGetBranches(streamId, ServerLimits.OLD_BRANCH_GET_LIMIT, commitsLimit)
        .ConfigureAwait(true);
    }

    return branches;
  }

  /// <summary>
  /// Get branches from a given stream
  /// </summary>
  /// <param name="streamId">Id of the stream to get the branches from</param>
  /// <param name="branchesLimit">Max number of branches to retrieve</param>
  /// <param name="commitsLimit">Max number of commits to retrieve</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <seealso cref="GraphQL.Resources.ProjectResource.GetWithModels"/>
  /// <seealso cref="GraphQL.Resources.ModelResource.GetModels"/>
  [Obsolete($"Use client.{nameof(Model)}.{nameof(ModelResource.GetModels)}")]
  public async Task<List<Branch>> StreamGetBranches(
    string streamId,
    int branchesLimit = 10,
    int commitsLimit = 10,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
      query Stream ($streamId: String!, $branchesLimit: Int!, $commitsLimit: Int!, $includeCommits: Boolean!) {
        stream(id: $streamId) {
          branches(limit: $branchesLimit) {
            items {
              id
              name
              description
              commits (limit: $commitsLimit) @include(if: $includeCommits) {
                totalCount
                cursor
                items {
                  id
                  referencedObject
                  sourceApplication
                  message
                  authorName
                  authorId
                  parents
                  createdAt
                }
              }
            }
          }                       
        }
      }
      """;
    var request = new GraphQLRequest
    {
      Query = QUERY,
      Variables = new
      {
        streamId,
        branchesLimit,
        commitsLimit,
        includeCommits = commitsLimit > 0,
      }
    };
    var res = await ExecuteGraphQLRequest<StreamData>(request, cancellationToken).ConfigureAwait(false);

    //faster than making the server query for it for every single commit...
    foreach (var branch in res.stream.branches?.items)
    {
      if (branch.commits?.items is null)
      {
        continue;
      }

      foreach (var commit in branch.commits.items)
      {
        commit.branchName = branch.name;
      }
    }

    return res.stream.branches.items;
  }

  /// <summary>
  /// Creates a branch on a stream.
  /// </summary>
  /// <param name="branchInput"></param>
  /// <param name="cancellationToken"></param>
  /// <returns>The branch id.</returns>
  /// <seealso cref="GraphQL.Resources.ModelResource.Create"/>
  [Obsolete($"Use client.{nameof(Model)}.{nameof(ModelResource.Create)}")]
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
  /// <returns>The requested branch, no <see langword="null"/> if there was no branch with the given <paramref name="branchName"/></returns>
  /// <remarks>Updated to Model.GetWithVersions</remarks>
  /// <seealso cref="GraphQL.Resources.ModelResource.Get"/>
  /// <seealso cref="GraphQL.Resources.ModelResource.GetWithVersions"/>
  [Obsolete($"Use client.{nameof(Model)}.{nameof(ModelResource.Get)}")]
  public async Task<Branch?> BranchGet(
    string streamId,
    string branchName,
    int commitsLimit = 10,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
      query Stream($streamId: String!, $branchName: String!, $commitsLimit: Int!, $includeCommits: Boolean!) {
        stream(id: $streamId) {
          branch(name: $branchName){
            id,
            name,
            description,
            commits (limit: $commitsLimit) @include(if: $includeCommits) {
              totalCount,
              cursor,
              items {
                id,
                referencedObject,
                sourceApplication,
                totalChildrenCount,
                message,
                authorName,
                authorId,
                parents,
                createdAt
              }
            }
          }                       
        }
      }
      """;

    var request = new GraphQLRequest
    {
      Query = QUERY,
      Variables = new
      {
        streamId,
        branchName,
        commitsLimit,
        includeCommits = commitsLimit > 0,
      }
    };

    var res = await ExecuteGraphQLRequest<StreamData>(request, cancellationToken).ConfigureAwait(false);
    var branch = res.stream.branch;

    if (branch?.commits?.items is not null)
    {
      //faster than making the server query for it for every single commit...
      foreach (var commit in branch.commits.items)
      {
        commit.branchName = branch.name;
      }
    }

    return res.stream.branch;
  }

  /// <summary>
  /// Gets a given model from a project.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <param name="projectId">Id of the project to get the model from</param>
  /// <param name="modelId">Id of the model</param>
  /// <returns></returns>
  /// <seealso cref="GraphQL.Resources.ModelResource.Get"/>
  [Obsolete($"Use client.{nameof(Model)}.{nameof(ModelResource.Get)}")]
  public async Task<Branch> ModelGet(string projectId, string modelId, CancellationToken cancellationToken = default)
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

    var res = await ExecuteGraphQLRequest<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(
        request,
        cancellationToken
      )
      .ConfigureAwait(false);
    var branch = new Branch
    {
      description = res["project"]["model"]["description"],
      id = res["project"]["model"]["id"],
      name = res["project"]["model"]["name"]
    };
    return branch;
  }

  /// <summary>
  /// Updates a branch.
  /// </summary>
  /// <param name="branchInput"></param>
  /// <returns>The stream's id.</returns>
  /// <seealso cref="GraphQL.Resources.ModelResource.Update"/>
  [Obsolete($"Use client.{nameof(Model)}.{nameof(ModelResource.Update)}")]
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
  /// <seealso cref="GraphQL.Resources.ModelResource.Delete"/>
  [Obsolete($"Use client.{nameof(Model)}.{nameof(ModelResource.Delete)}")]
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
