using GraphQL;
using System.Threading.Tasks;
using System.Threading;
using GraphQL.Client.Http;
using System.Linq;
using Speckle.Core.Api.GraphQL.Models;
using Speckle.Core.Api.GraphQL.Models.Responses;

namespace Speckle.Core.Api.GraphQL;

public static class GraphQLHttpClientExtensions
{
  /// <summary>
  /// Gets the version of the current server. Useful for guarding against unsupported api calls on newer or older servers.
  /// </summary>
  /// <param name="cancellationToken">[Optional] defaults to an empty cancellation token</param>
  /// <returns><see cref="Version"/> object excluding any strings (eg "2.7.2-alpha.6995" becomes "2.7.2.6995")</returns>
  /// <exception cref="SpeckleGraphQLException{ServerInfoResponse}"></exception>
  public static async Task<System.Version> GetServerVersion(
    this GraphQLHttpClient client,
    CancellationToken cancellationToken = default
  )
  {
    var request = new GraphQLRequest
    {
      Query =
        @"query Server {
                    serverInfo {
                        version
                      }
                  }"
    };

    var response = await client.SendQueryAsync<ServerInfoResponse>(request, cancellationToken).ConfigureAwait(false);

    if (response.Errors != null)
    {
      throw new SpeckleGraphQLException<ServerInfoResponse>(
        $"Query {nameof(GetServerVersion)} failed",
        request,
        response
      );
    }

    if (string.IsNullOrWhiteSpace(response.Data.serverInfo.version))
    {
      throw new SpeckleGraphQLException<ServerInfoResponse>(
        $"Query {nameof(GetServerVersion)} did not provide a valid server version",
        request,
        response
      );
    }

    return response.Data.serverInfo.version == "dev"
      ? new System.Version(999, 999, 999)
      : new System.Version(response.Data.serverInfo.version.Split('-').First());
  }
}
