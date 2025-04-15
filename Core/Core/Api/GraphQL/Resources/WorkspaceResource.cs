using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Models.Responses;

namespace Speckle.Core.Api.GraphQL.Resources;

public sealed class WorkspaceResource
{
  private readonly ISpeckleGraphQLClient _client;

  internal WorkspaceResource(ISpeckleGraphQLClient client)
  {
    _client = client;
  }

  /// <param name="workspaceId"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  /// <inheritdoc cref="ISpeckleGraphQLClient.ExecuteGraphQLRequest{T}"/>
  public async Task<Workspace> Get(string workspaceId, CancellationToken cancellationToken = default)
  {
    //language=graphql
    const string QUERY = """
      query WorkspaceGet($workspaceId: String!) {
        data:workspace(id: $workspaceId) {
          id
          name
          role
          slug
          description
          permissions {
            canCreateProject {
              authorized
              code
              message
            }
          }
        }
      }
      """;
    GraphQLRequest request = new() { Query = QUERY, Variables = new { workspaceId } };

    var response = await _client
      .ExecuteGraphQLRequest<RequiredResponse<Workspace>>(request, cancellationToken)
      .ConfigureAwait(false);
    return response.data;
  }
}
