using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Models.Responses;

namespace Speckle.Core.Api.GraphQL.Resources;

public sealed class ProjectResource
{
  private readonly ISpeckleGraphQLClient _client;

  internal ProjectResource(ISpeckleGraphQLClient client)
  {
    _client = client;
  }

  /// <param name="projectId"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<Project> Get(string projectId, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
                         query Project($projectId: String!) {
                           project(id: $projectId) {
                             id
                             name
                             description
                             visibility
                             allowPublicComments
                             role
                             createdAt
                             updatedAt
                             sourceApps
                           }
                         }
                         """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { projectId } };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectResponse>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.project;
  }

  /// <param name="projectId"></param>
  /// <param name="modelsLimit"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<Project> GetWithModels(
    string projectId,
    int modelsLimit,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
                         query ProjectGetWithModels($projectId: String!, $modelsLimit: Int!) {
                           project(id: $projectId) {
                             id
                             name
                             description
                             visibility
                             allowPublicComments
                             role
                             createdAt
                             updatedAt
                             sourceApps
                             models(limit: $modelsLimit) {
                               items {
                                 id
                                 name
                                 previewUrl
                                 updatedAt
                                 displayName
                                 description
                                 createdAt
                               }
                               cursor
                               totalCount
                             }
                           }
                         }
                         """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { projectId, modelsLimit } };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectResponse>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.project;
  }

  /// <param name="projectId"></param>
  /// <param name="modelsLimit"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<Project> GetWithTeam(
    string projectId,
    int modelsLimit,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
                         query ProjectGetWithTeam($projectId: String!) {
                           project(id: $projectId) {
                             id
                             name
                             description
                             visibility
                             allowPublicComments
                             role
                             createdAt
                             updatedAt
                             team {
                               role
                               user {
                                 totalOwnedStreamsFavorites
                                 id
                                 name
                                 bio
                                 company
                                 avatar
                                 verified
                                 role
                               }
                             }
                             invitedTeam {
                               id
                               inviteId
                               projectId
                               projectName
                               streamId
                               streamName
                               title
                               role
                               token
                               user {
                                 totalOwnedStreamsFavorites
                                 id
                                 name
                                 bio
                                 company
                                 avatar
                                 verified
                                 role
                               }
                               invitedBy {
                                 totalOwnedStreamsFavorites
                                 id
                                 name
                                 bio
                                 company
                                 avatar
                                 verified
                                 role
                               }
                             }
                           }
                         }
                         """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { projectId, modelsLimit } };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectResponse>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.project;
  }

  /// <param name="input"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<Project> Create(ProjectCreateInput input, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
                         mutation ProjectCreate($input: ProjectCreateInput) {
                           projectMutations {
                             create(input: $input) {
                               id
                               name
                               description
                               visibility
                               allowPublicComments
                               role
                               createdAt
                               updatedAt
                               sourceApps
                             }
                           }
                         }
                         """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { input } };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectMutationResponse>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.projectMutations.create;
  }

  /// <param name="input"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<Project> Update(ProjectUpdateInput input, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
                         mutation ProjectUpdate($input: ProjectUpdateInput!) {
                           projectMutations{
                             update(update: $input) {
                               id
                               name
                               description
                               visibility
                               allowPublicComments
                               role
                               createdAt
                               updatedAt
                             }
                           }
                         }
                         """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { input } };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectMutationResponse>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.projectMutations.update;
  }

  /// <param name="deleteId"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<bool> Delete(string deleteId, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
                         mutation ProjectDelete($deleteId: String!) {
                           projectMutations {
                             delete(id: $deleteId)
                           }
                         }
                         """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { deleteId } };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectMutationResponse>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.projectMutations.delete;
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="input"></param>
  /// <param name="cancellationToken"></param>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<Project> UpdateRole(ProjectUpdateRoleInput input, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
                         mutation ProjectUpdateRole($input: ProjectUpdateRoleInput!) {
                           projectMutations {
                             updateRole(input: $input) {
                               id
                               name
                               description
                               visibility
                               allowPublicComments
                               role
                               createdAt
                               updatedAt
                               team {
                                 role
                                 user {
                                   totalOwnedStreamsFavorites
                                   id
                                   name
                                   bio
                                   company
                                   avatar
                                   verified
                                   role
                                 }
                               }
                               invitedTeam {
                                 id
                                 inviteId
                                 projectId
                                 projectName
                                 streamId
                                 streamName
                                 title
                                 role
                                 token
                                 user {
                                   totalOwnedStreamsFavorites
                                   id
                                   name
                                   bio
                                   company
                                   avatar
                                   verified
                                   role
                                 }
                                 invitedBy {
                                   totalOwnedStreamsFavorites
                                   id
                                   name
                                   bio
                                   company
                                   avatar
                                   verified
                                   role
                                 }
                               }
                             }
                           }
                         }
                         """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { input } };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectMutationResponse>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.projectMutations.updateRole;
  }
}
