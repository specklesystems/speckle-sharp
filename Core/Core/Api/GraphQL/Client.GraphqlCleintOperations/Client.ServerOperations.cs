#nullable enable

using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Speckle.Core.Logging;

namespace Speckle.Core.Api;

public partial class Client
{
  /// <summary>
  /// Gets the version of the current server. Useful for guarding against unsupported api calls on newer or older servers.
  /// </summary>
  /// <param name="cancellationToken">[Optional] defaults to an empty cancellation token</param>
  /// <returns><see cref="Version"/> object excluding any strings (eg "2.7.2-alpha.6995" becomes "2.7.2.6995")</returns>
  /// <exception cref="SpeckleException"></exception>
  ///
  public async Task<System.Version> GetServerVersion(CancellationToken cancellationToken = default)
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

    var res = await ExecuteGraphQLRequest<ServerInfoResponse>(request, cancellationToken).ConfigureAwait(false);

    if (res.serverInfo.version.Contains("dev"))
      return new System.Version(999, 999, 999);

    ServerVersion = new System.Version(Regex.Replace(res.serverInfo.version, "[-a-zA-Z]+", ""));
    return ServerVersion;
  }
}
