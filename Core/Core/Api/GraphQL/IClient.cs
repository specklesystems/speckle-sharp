using System.Threading;
using System.Threading.Tasks;
using GraphQL;

namespace Speckle.Core.Api.GraphQL;

internal interface ISpeckleClient
{
  public Task<T> ExecuteGraphQLRequest<T>(GraphQLRequest request, CancellationToken cancellationToken);
}
