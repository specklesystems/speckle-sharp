using System;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;

namespace Speckle.Core.Api.GraphQL;

internal interface ISpeckleGraphQLClient
{
  public Task<T> ExecuteGraphQLRequest<T>(GraphQLRequest request, CancellationToken cancellationToken);
}

internal interface ISpeckleGraphQLSubscriber : ISpeckleGraphQLClient
{
  internal IDisposable SubscribeTo<T>(GraphQLRequest request, Action<object, T> callback);
}
