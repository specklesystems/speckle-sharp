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

  /// <remarks>Requires server version >=2.20.6</remarks>
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
          workspaceId
        }
      }
      """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { projectId } };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectResponse>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.project;
  }

  /// <remarks>Requires server version >=2.20.6</remarks>
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
          workspaceId
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
  /// <returns><see langword="null"/> if the server responds with an error, to support older server versions easier</returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<ProjectPermissionChecks?> GetPermissions(
    string projectId,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
      query Project($projectId: String!) {
        data:project(id: $projectId) {
          data:permissions {
            canCreateModel {
              authorized
              code
              message
            }
            canDelete {
              authorized
              code
              message
            }
            canLoad {
              authorized
              code
              message
            }
            canPublish {
              authorized
              code
              message
            }
          }
        }
      }
      """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { projectId } };

    try
    {
      var response = await _client
        .ExecuteGraphQLRequest<RequiredResponse<RequiredResponse<ProjectPermissionChecks>>>(request, cancellationToken)
        .ConfigureAwait(false);
      return response.data.data;
    }
    catch (SpeckleGraphQLException)
    {
      //Expecting older server versions to not have the permission check in the schema
      return null;
    }
  }

  /// <remarks>Requires server version >=2.20.6</remarks>
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
          workspaceId
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

  /// <summary>
  /// Creates a non-workspace project (aka Personal Project)
  /// </summary>
  /// <remarks>Requires server version >=2.20.6</remarks>
  /// <seealso cref="ActiveUserResource.CanCreatePersonalProjects"/>
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
            workspaceId
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

  /// <summary>
  /// Creates a workspace project ()
  /// </summary>
  /// <remarks>
  /// This feature is only supported by Workspace Enabled Servers (e.g. app.speckle.systems).
  /// A <see cref="Workspace"/>'s <see cref="Workspace.permissions"/> list can be checked if the user <see cref="WorkspacePermissionChecks.canCreateProject"/>.
  /// </remarks>
  /// <param name="input"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<Project> CreateInWorkspace(
    WorkspaceProjectCreateInput input,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
      mutation WorkspaceProjectCreate($input: WorkspaceProjectCreateInput!) {
        data:workspaceMutations {
          data:projects {
            data:create(input: $input) {
              id
              name
              description
              visibility
              allowPublicComments
              role
              createdAt
              updatedAt
              sourceApps
              workspaceId
            }
          }
        }
      }
      """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { input } };

    var response = await _client
      .ExecuteGraphQLRequest<RequiredResponse<RequiredResponse<RequiredResponse<Project>>>>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.data.data.data;
  }

  /// <param name="input"></param>
  /// <remarks>Requires server version >=2.20.6</remarks>
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
            workspaceId
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

  /// <remarks>Requires server version >=2.20.6</remarks>
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
            workspaceId
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
