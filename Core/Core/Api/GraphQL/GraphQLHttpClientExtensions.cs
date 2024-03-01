using GraphQL;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using GraphQL.Client.Http;

namespace Speckle.Core.Api.GraphQL;

public static class GraphQLHttpClientExtensions
{
  /// <summary>
  /// Gets the version of the current server. Useful for guarding against unsupported api calls on newer or older servers.
  /// </summary>
  /// <param name="cancellationToken">[Optional] defaults to an empty cancellation token</param>
  /// <returns><see cref="Version"/> object excluding any strings (eg "2.7.2-alpha.6995" becomes "2.7.2.6995")</returns>
  /// <exception cref="SpeckleGraphQLException"></exception>
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

    return response.Data.serverInfo.version == "dev"
      ? new System.Version(999, 999, 999)
      : new System.Version(Regex.Replace(response.Data.serverInfo.version, "[-a-zA-Z]+", ""));
  }
}
