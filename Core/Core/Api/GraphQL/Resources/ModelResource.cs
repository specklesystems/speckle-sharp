using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Models.Responses;

namespace Speckle.Core.Api.GraphQL.Resources;

public sealed class ModelResource
{
  private readonly ISpeckleGraphQLClient _client;

  internal ModelResource(ISpeckleGraphQLClient client)
  {
    _client = client;
  }

  /// <param name="modelId"></param>
  /// <param name="projectId"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  /// <seealso cref="GetWithVersions"/>
  public async Task<Model> Get(string modelId, string projectId, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
                         query ModelGet($modelId: String!, $projectId: String!) {
                           project(id: $projectId) {
                             model(id: $modelId) {
                               id
                               name
                               previewUrl
                               updatedAt
                               description
                               displayName
                               createdAt
                               author {
                                 avatar
                                 bio
                                 company
                                 id
                                 name
                                 role
                                 totalOwnedStreamsFavorites
                                 verified
                               }
                             }
                           }
                         }
                         """;
    var request = new GraphQLRequest { Query = QUERY, Variables = new { modelId, projectId } };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectResponse>(request, cancellationToken)
      .ConfigureAwait(false);

    return response.project.model;
  }

  /// <param name="projectId"></param>
  /// <param name="modelId"></param>
  /// <param name="versionsLimit">Max number of versions to fetch</param>
  /// <param name="versionsCursor">Optional cursor for pagination</param>
  /// <param name="versionsFilter">Optional versions filter</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  /// <see cref="Get"/>
  public async Task<Model> GetWithVersions(
    string modelId,
    string projectId,
    int versionsLimit = ServerLimits.DEFAULT_PAGINATION_REQUEST,
    string? versionsCursor = null,
    ModelVersionsFilter? versionsFilter = null,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
                         query ModelGetWithVersions($modelId: String!, $projectId: String!, $versionsLimit: Int!, $versionsCursor: String, $versionsFilter: ModelVersionsFilter) {
                           project(id: $projectId) {
                             model(id: $modelId) {
                               id
                               name
                               previewUrl
                               updatedAt
                               versions(limit: $versionsLimit, cursor: $versionsCursor, filter: $versionsFilter) {
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
                                   }
                                 }
                                 totalCount
                                 cursor
                               }
                               description
                               displayName
                               createdAt
                               author {
                                 avatar
                                 bio
                                 company
                                 id
                                 name
                                 role
                                 totalOwnedStreamsFavorites
                                 verified
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
        projectId,
        modelId,
        versionsLimit,
        versionsCursor,
        versionsFilter,
      }
    };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectResponse>(request, cancellationToken)
      .ConfigureAwait(false);

    return response.project.model;
  }

  /// <param name="projectId"></param>
  /// <param name="modelsLimit">Max number of models to fetch</param>
  /// <param name="modelsCursor">Optional cursor for pagination</param>
  /// <param name="modelsFilter">Optional models filter</param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<ResourceCollection<Model>> GetModels(
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
                               totalCount
                               cursor
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
    return response.project.models;
  }

  /// <param name="input"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<Model> Create(CreateModelInput input, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
                         mutation ModelCreate($input: CreateModelInput!) {
                           modelMutations {
                             create(input: $input) {
                               id
                               displayName
                               name
                               description
                               createdAt
                               updatedAt
                               previewUrl
                               author {
                                 avatar
                                 bio
                                 company
                                 id
                                 name
                                 role
                                 totalOwnedStreamsFavorites
                                 verified
                               }
                             }
                           }
                         }
                         """;

    GraphQLRequest request = new() { Query = QUERY, Variables = new { input } };

    var res = await _client
      .ExecuteGraphQLRequest<ModelMutationResponse>(request, cancellationToken)
      .ConfigureAwait(false);

    return res.modelMutations.create;
  }

  /// <param name="input"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<bool> Delete(DeleteModelInput input, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
                         mutation ModelDelete($input: DeleteModelInput!) {
                           modelMutations {
                             delete(input: $input)
                           }
                         }
                         """;

    GraphQLRequest request = new() { Query = QUERY, Variables = new { input } };

    var res = await _client
      .ExecuteGraphQLRequest<ModelMutationResponse>(request, cancellationToken)
      .ConfigureAwait(false);

    return res.modelMutations.delete;
  }

  /// <param name="input"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<Model> Update(UpdateModelInput input, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
                         mutation ModelUpdate($input: UpdateModelInput!) {
                           modelMutations {
                             update(input: $input) {
                               id
                               name
                               displayName
                               description
                               createdAt
                               updatedAt
                               previewUrl
                               author {
                                 avatar
                                 bio
                                 company
                                 id
                                 name
                                 role
                                 totalOwnedStreamsFavorites
                                 verified
                               }
                             }
                           }
                         }
                         """;

    GraphQLRequest request = new() { Query = QUERY, Variables = new { input } };

    var res = await _client
      .ExecuteGraphQLRequest<ModelMutationResponse>(request, cancellationToken)
      .ConfigureAwait(false);

    return res.modelMutations.update;
  }
}
