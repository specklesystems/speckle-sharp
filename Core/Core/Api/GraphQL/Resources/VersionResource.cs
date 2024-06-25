using System;
using System.Collections.Generic;
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
    string projectId,
    string modelId,
    string versionId,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
                         query VersionsGet($projectId: String!, $modelId: String!, $versionId: String!) {
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
  /// <param name="limit"></param>
  /// <param name="cancellationToken"></param>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<List<Version>> GetVersions(
    string projectId,
    string modelId,
    int limit = 25,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
                         query VersionGetVersions($projectId: String!, $modelId: String!, $limit: Int!) {
                           project(id: $projectId) {
                             model(id: $modelId) {
                               versions(limit: $limit) {
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
          limit
        }
      };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectResponse>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.project.model.versions.items;
  }

  //TODO: Implement on server
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<Version> Create(CancellationToken cancellationToken = default)
  {
    throw new NotImplementedException("Not Implemented on Server");
    //language=graphql
    const string QUERY = """

                         """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { } };

    var response = await _client.ExecuteGraphQLRequest<object>(request, cancellationToken).ConfigureAwait(false);
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
      .ExecuteGraphQLRequest<Dictionary<string, Version>>(request, cancellationToken)
      .ConfigureAwait(false);
    return response["Update"];
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
      .ExecuteGraphQLRequest<Dictionary<string, Model>>(request, cancellationToken)
      .ConfigureAwait(false);
    return response["moveToModel"].id;
  }

  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<bool> Delete(CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
                         mutation VersionDelete($input: DeleteVersionsInput!) {
                           versionMutations {
                             delete(input: $input)
                           }
                         }
                         """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { } };

    var response = await _client
      .ExecuteGraphQLRequest<Dictionary<string, bool>>(request, cancellationToken)
      .ConfigureAwait(false);
    return response["delete"];
  }

  //TODO: Implement on server

  /// <param name="commitReceivedInput"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<bool> Received(
    CommitReceivedInput commitReceivedInput,
    CancellationToken cancellationToken = default
  )
  {
    throw new NotImplementedException("Not implemented on server");
    //language=graphql
    const string QUERY = """
                         
                         """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { } };

    var response = await _client.ExecuteGraphQLRequest<object>(request, cancellationToken).ConfigureAwait(false);
  }
}
