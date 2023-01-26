#nullable enable

using System.Collections.Generic;
using System.Linq;
using GraphQL;
using Speckle.Core.Logging;

namespace Speckle.Core.Api
{
  /// <summary>
  /// Base class for GraphQL API exceptions
  /// </summary>
  public class SpeckleGraphQLException<T> : SpeckleException
  {
    private GraphQLRequest _request;
    private GraphQLResponse<T> _response;

    public IEnumerable<string> ErrorMessages => _response.Errors.Select(e => e.Message);
    public IDictionary<string, object>? Extensions => _response.Extensions;

    public SpeckleGraphQLException(
      string message,
      GraphQLRequest request,
      GraphQLResponse<T> response
    ) : base(message)
    {
      _request = request;
      _response = response;
    }
  }

  /// <summary>
  /// Represents a "FORBIDDEN" on "UNAUTHORIZED" GraphQL error as an exception.
  /// https://www.apollographql.com/docs/apollo-server/v2/data/errors/#unauthenticated
  /// https://www.apollographql.com/docs/apollo-server/v2/data/errors/#forbidden
  /// </summary>
  public class SpeckleGraphQLForbiddenException<T> : SpeckleGraphQLException<T>
  {
    public SpeckleGraphQLForbiddenException(GraphQLRequest request, GraphQLResponse<T> response)
      : base("Your request was forbidden", request, response) { }
  }

  public class SpeckleGraphQLInternalErrorException<T> : SpeckleGraphQLException<T>
  {
    public SpeckleGraphQLInternalErrorException(GraphQLRequest request, GraphQLResponse<T> response)
      : base("Your request failed on the server side", request, response) { }
  }
}
