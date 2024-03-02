using System.Threading;
using System.Threading.Tasks;
using GraphQL;

namespace Speckle.Core.Api.GraphQL;

internal interface ISpeckleGraphQLClient
{
  public Task<T> ExecuteGraphQLRequest<T>(GraphQLRequest request, CancellationToken cancellationToken);
}
