using System;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;

namespace Speckle.Core.Api.GraphQL;

internal interface ISpeckleGraphQLClient
{
  /// <exception cref="SpeckleGraphQLForbiddenException">"FORBIDDEN" on "UNAUTHORIZED" response from server</exception>
  /// <exception cref="SpeckleGraphQLException">All other request errors</exception>
  /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken"/> requested a cancel</exception>
  /// <exception cref="ObjectDisposedException">This <see cref="Client"/> already been disposed</exception>
  internal Task<T> ExecuteGraphQLRequest<T>(GraphQLRequest request, CancellationToken cancellationToken);

  /// <exception cref="SpeckleGraphQLForbiddenException">"FORBIDDEN" on "UNAUTHORIZED" response from server</exception>
  /// <exception cref="SpeckleGraphQLException">All other request errors</exception>
  /// <exception cref="ObjectDisposedException">This <see cref="Client"/> already been disposed</exception>
  internal IDisposable SubscribeTo<T>(GraphQLRequest request, Action<object, T> callback);
}
