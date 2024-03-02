using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;

namespace Speckle.Core.Api.GraphQL.Resources;

public sealed class ProjectResource
{
  private readonly ISpeckleGraphQLClient _client;

  internal ProjectResource(ISpeckleGraphQLClient client)
  {
    _client = client;
  }

  //TODO: Figure out operation name, should we use the `GraphQLRequest` ctor arg or bake in string?
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

    var response = await _client.ExecuteGraphQLRequest<ProjectData>(request, cancellationToken).ConfigureAwait(false);
    return response.project;
  }

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

    var response = await _client.ExecuteGraphQLRequest<ProjectData>(request, cancellationToken).ConfigureAwait(false);
    return response.project;
  }

  //TODO: Double check that this covers both seealso tagged functions
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

    var response = await _client.ExecuteGraphQLRequest<ProjectData>(request, cancellationToken).ConfigureAwait(false);
    return response.project;
  }

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

  public async Task<Project> Update(ProjectUpdateInput input, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
                         mutation ProjectUpdate($update: ProjectUpdateInput!) {
                           data:projectMutations{
                             update(update: $update) {
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
      .ExecuteGraphQLRequest<Dictionary<string, bool>>(request, cancellationToken)
      .ConfigureAwait(false);
    return response["projectMutations"];
  }

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
      .ExecuteGraphQLRequest<Dictionary<string, dynamic>>(request, cancellationToken)
      .ConfigureAwait(false);
    return response["projectMutations"];
  }
}
