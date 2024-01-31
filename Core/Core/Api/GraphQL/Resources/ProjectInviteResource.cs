using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Api.GraphQL.Inputs;
using Speckle.Core.Api.GraphQL.Models;

namespace Speckle.Core.Api.GraphQL.Resources;

public sealed class ProjectInviteResource
{
  private readonly ISpeckleClient _client;

  internal ProjectInviteResource(ISpeckleClient client)
  {
    _client = client;
  }

  //TODO: Do we really want to return the entire project here?
  //previously we returned a bool
  //but there doesn't appear to be a nice way to return the invite id (assuming we'd want to)
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
      .ExecuteGraphQLRequest<Dictionary<string, Project>>(request, cancellationToken)
      .ConfigureAwait(false);
    return response["create"];
  }

  //TODO: what to return...
  public async Task Use(ProjectInviteUseInput input, CancellationToken cancellationToken = default)
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
      .ExecuteGraphQLRequest<Dictionary<string, object>>(request, cancellationToken)
      .ConfigureAwait(false);
    throw new NotImplementedException("figure out what to return here");
  }

  //TODO: again, what to return...
  public async Task<bool> Cancel(string projectId, string inviteId, CancellationToken cancellationToken = default)
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
      .ExecuteGraphQLRequest<Dictionary<string, bool>>(request, cancellationToken)
      .ConfigureAwait(false);
    return response["cancel"];
  }

  public async Task<Project> BatchCreate(
    string projectId,
    IReadOnlyList<ProjectInviteCreateInput> input,
    CancellationToken cancellationToken = default
  )
  {
    //language=graphql
    const string QUERY = """
                         mutation ProjectInviteBatchCreate($projectId: ID!, $input: [ProjectInviteCreateInput!]!) {
                           projectMutations {
                             invites {
                               batchCreate(projectId: $projectId, input: $input) {
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
      .ExecuteGraphQLRequest<Dictionary<string, Project>>(request, cancellationToken)
      .ConfigureAwait(false);
    return response["batchCreate"];
  }
}
