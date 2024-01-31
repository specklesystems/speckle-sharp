using System.Threading;
using System.Threading.Tasks;
using GraphQL;

namespace Speckle.Core.Api.GraphQL.Resources;

public sealed class ActiveUserResource
{
  private readonly ISpeckleClient _client;

  internal ActiveUserResource(ISpeckleClient client)
  {
    _client = client;
  }

  /// <summary>
  /// Gets the currently active user profile.
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public async Task<Models.User> ActiveUserGet(CancellationToken cancellationToken = default)
  {
    var request = new GraphQLRequest
    {
      Query =
        @"query User {
                      activeUser {
                        id,
                        email,
                        name,
                        bio,
                        company,
                        avatar,
                        verified,
                        profiles,
                        role,
                      }
                    }"
    };
    return (await ExecuteGraphQLRequest<ActiveUserData>(request, cancellationToken).ConfigureAwait(false)).activeUser;
  }
}
