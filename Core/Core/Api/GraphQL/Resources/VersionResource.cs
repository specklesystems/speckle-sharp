using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Models.Responses;
using Version = Speckle.Core.Api.GraphQL.Models.Version;

namespace Speckle.Core.Api.GraphQL.Resources;

public sealed class VersionResource
{
  private readonly ISpeckleGraphQLClient _client;

  internal VersionResource(ISpeckleGraphQLClient client)
  {
    _client = client;
  }

  /// <param name="projectId"></param>
  /// <param name="modelId"></param>
  /// <param name="versionId"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<Version> Get(
    string versionId,
    string modelId,
    string projectId,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
      query VersionGet($projectId: String!, $modelId: String!, $versionId: String!) {
        project(id: $projectId) {
          model(id: $modelId) {
            version(id: $versionId) {
              id
              referencedObject
              message
              sourceApplication
              createdAt
              previewUrl
              authorUser {
                totalOwnedStreamsFavorites
                id
                name
                bio
                company
                verified
                role
                avatar
              }
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
          modelId,
          versionId
        }
      };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectResponse>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.project.model.version;
  }

  /// <param name="projectId"></param>
  /// <param name="modelId"></param>
  /// <param name="limit">Max number of versions to fetch</param>
  /// <param name="cursor">Optional cursor for pagination</param>
  /// <param name="filter">Optional filter</param>
  /// <param name="cancellationToken"></param>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<ResourceCollection<Version>> GetVersions(
    string modelId,
    string projectId,
    int limit = ServerLimits.DEFAULT_PAGINATION_REQUEST,
    string? cursor = null,
    ModelVersionsFilter? filter = null,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
      query VersionGetVersions($projectId: String!, $modelId: String!, $limit: Int!, $cursor: String, $filter: ModelVersionsFilter) {
        project(id: $projectId) {
          model(id: $modelId) {
            versions(limit: $limit, cursor: $cursor, filter: $filter) {
              items {
                id
                referencedObject
                message
                sourceApplication
                createdAt
                previewUrl
                authorUser {
                  totalOwnedStreamsFavorites
                  id
                  name
                  bio
                  company
                  verified
                  role
                  avatar
                }
              }
              cursor
              totalCount
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
          modelId,
          limit,
          cursor,
          filter,
        }
      };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectResponse>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.project.model.versions;
  }

  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<string> Create(CommitCreateInput input, CancellationToken cancellationToken = default)
  {
    //TODO: Implement on server
    return await ((Client)_client).CommitCreate(input, cancellationToken).ConfigureAwait(false);
  }

  /// <param name="input"></param>
  /// <param name="cancellationToken"></param>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<Version> Update(UpdateVersionInput input, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
      mutation VersionUpdate($input: UpdateVersionInput!) {
        versionMutations {
          update(input: $input) {
            id
            referencedObject
            message
            sourceApplication
            createdAt
            previewUrl
            authorUser {
              totalOwnedStreamsFavorites
              id
              name
              bio
              company
              verified
              role
              avatar
            }
          }
        }
      }
      """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { input, } };

    var response = await _client
      .ExecuteGraphQLRequest<VersionMutationResponse>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.versionMutations.update;
  }

  //TODO: Would we rather return the full model here? with or with out versions?
  /// <param name="input"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<string> MoveToModel(MoveVersionsInput input, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
      mutation VersionMoveToModel($input: MoveVersionsInput!) {
        versionMutations {
          moveToModel(input: $input) {
            id
          }
        }
      }
      """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { input, } };

    var response = await _client
      .ExecuteGraphQLRequest<VersionMutationResponse>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.versionMutations.moveToModel.id;
  }

  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<bool> Delete(DeleteVersionsInput input, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
      mutation VersionDelete($input: DeleteVersionsInput!) {
        versionMutations {
          delete(input: $input)
        }
      }
      """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { input } };

    var response = await _client
      .ExecuteGraphQLRequest<VersionMutationResponse>(request, cancellationToken)
      .ConfigureAwait(false);

    return response.versionMutations.delete;
  }

  /// <param name="commitReceivedInput"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<bool> Received(
    CommitReceivedInput commitReceivedInput,
    CancellationToken cancellationToken = default
  )
  {
    //TODO: Implement on server
    return await ((Client)_client).CommitReceived(commitReceivedInput, cancellationToken).ConfigureAwait(false);
  }
}
