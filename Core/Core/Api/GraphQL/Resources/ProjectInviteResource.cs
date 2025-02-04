using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Models.Responses;

namespace Speckle.Core.Api.GraphQL.Resources;

public sealed class ProjectInviteResource
{
  private readonly ISpeckleGraphQLClient _client;

  internal ProjectInviteResource(ISpeckleGraphQLClient client)
  {
    _client = client;
  }

  /// <param name="projectId"></param>
  /// <param name="input"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<Project> Create(
    string projectId,
    ProjectInviteCreateInput input,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
      mutation ProjectInviteCreate($projectId: ID!, $input: ProjectInviteCreateInput!) {
        projectMutations {
          invites {
            create(projectId: $projectId, input: $input) {
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
                streamName
                title
                role
                streamId
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
      }
      """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { projectId, input } };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectMutationResponse>(request, cancellationToken)
      .ConfigureAwait(false);

    return response.projectMutations.invites.create;
  }

  /// <param name="input"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<bool> Use(ProjectInviteUseInput input, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
      mutation ProjectInviteUse($input: ProjectInviteUseInput!) {
        projectMutations {
          invites {
            use(input: $input)
          }
        }
      }
      """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { input } };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectMutationResponse>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.projectMutations.invites.use;
  }

  /// <param name="projectId"></param>
  /// <param name="token"></param>
  /// <param name="cancellationToken"></param>
  /// <returns>The invite, or null if no invite exists</returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<PendingStreamCollaborator?> Get(
    string projectId,
    string? token,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
      query ProjectInvite($projectId: String!, $token: String) {
        projectInvite(projectId: $projectId, token: $token) {
          id
          inviteId
          invitedBy {
            avatar
            bio
            company
            id
            name
            role
            totalOwnedStreamsFavorites
            verified
          }
          projectId
          projectName
          role
          streamId
          streamName
          title
          token
          user {
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
      """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { projectId, token } };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectInviteResponse>(request, cancellationToken)
      .ConfigureAwait(false);

    return response.projectInvite;
  }

  /// <param name="projectId"></param>
  /// <param name="inviteId"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<Project> Cancel(string projectId, string inviteId, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
      mutation ProjectInviteCancel($projectId: ID!, $inviteId: String!) {
        projectMutations {
          invites {
            cancel(projectId: $projectId, inviteId: $inviteId) {
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
                streamName
                title
                role
                streamId
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
      }
      """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { projectId, inviteId } };

    var response = await _client
      .ExecuteGraphQLRequest<ProjectMutationResponse>(request, cancellationToken)
      .ConfigureAwait(false);

    return response.projectMutations.invites.cancel;
  }
}
