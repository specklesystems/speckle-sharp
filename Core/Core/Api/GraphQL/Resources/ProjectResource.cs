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
  /// <seealso cref="GetWithModels"/>
  /// <seealso cref="GetWithTeam"/>
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
  /// <param name="modelsLimit">Max number of models to fetch</param>
  /// <param name="modelsCursor">Optional cursor for pagination</param>
  /// <param name="modelsFilter">Optional models filter</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  /// <seealso cref="Get"/>
  /// <seealso cref="GetWithTeam"/>
  public async Task<Project> GetWithModels(
    string projectId,
    int modelsLimit = ServerLimits.DEFAULT_PAGINATION_REQUEST,
    string? modelsCursor = null,
    ProjectModelsFilter? modelsFilter = null,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
      query ProjectGetWithModels($projectId: String!, $modelsLimit: Int!, $modelsCursor: String, $modelsFilter: ProjectModelsFilter) {
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
          models(limit: $modelsLimit, cursor: $modelsCursor, filter: $modelsFilter) {
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
    GraphQLRequest request =
      new()
      {
        Query = QUERY,
        Variables = new
        {
          projectId,
          modelsLimit,
          modelsCursor,
          modelsFilter
        }
      };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectResponse>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.project;
  }

  /// <param name="projectId"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  /// <seealso cref="Get"/>
  /// <seealso cref="GetWithModels"/>
  public async Task<Project> GetWithTeam(string projectId, CancellationToken cancellationToken = default)
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
    GraphQLRequest request = new() { Query = QUERY, Variables = new { projectId } };

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

  /// <param name="projectId">The id of the Project to delete</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<bool> Delete(string projectId, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
      mutation ProjectDelete($projectId: String!) {
        projectMutations {
          delete(id: $projectId)
        }
      }
      """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { projectId } };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectMutationResponse>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.projectMutations.delete;
  }

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
