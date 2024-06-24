﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;

namespace Speckle.Core.Api.GraphQL.Resources;

public sealed class ModelResource
{
  private readonly ISpeckleGraphQLClient _client;

  internal ModelResource(ISpeckleGraphQLClient client)
  {
    _client = client;
  }

  public async Task<Model> Get(string projectId, string modelId, CancellationToken cancellationToken = default)
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
    var request = new GraphQLRequest { Query = QUERY, Variables = new { projectId, modelId } };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectResponse>(request, cancellationToken)
      .ConfigureAwait(false);

    return response.project.model;
  }

  //TODO: can we do this smarter with Skip/Include directives?
  public async Task<Model> GetWithVersions(
    string projectId,
    string modelId,
    int versionsLimit,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
                         query ModelGetWithVersions($modelId: String!, $projectId: String!, $versionsLimit: Int!) {
                           project(id: $projectId) {
                             model(id: $modelId) {
                               id
                               name
                               previewUrl
                               updatedAt
                               versions(limit: $versionsLimit) {
                                 items {
                                   id
                                   message
                                   previewUrl
                                   referencedObject
                                   sourceApplication
                                   createdAt
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
        versionsLimit
      }
    };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectResponse>(request, cancellationToken)
      .ConfigureAwait(false);

    return response.project.model;
  }

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
      .ExecuteGraphQLRequest<Dictionary<string, Model>>(request, cancellationToken)
      .ConfigureAwait(false);
    return res["created"];
  }

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
      .ExecuteGraphQLRequest<Dictionary<string, bool>>(request, cancellationToken)
      .ConfigureAwait(false);
    return res["delete"];
  }

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
      .ExecuteGraphQLRequest<Dictionary<string, Model>>(request, cancellationToken)
      .ConfigureAwait(false);
    return res["update"];
  }
}
