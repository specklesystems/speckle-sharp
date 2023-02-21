#nullable enable

using System;
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
    public GraphQLResponse<T>? Response;

    public IEnumerable<string> ErrorMessages =>
      Response?.Errors != null ? Response.Errors.Select(e => e.Message) : new string[] { };
    public IDictionary<string, object>? Extensions => Response?.Extensions;

    public SpeckleGraphQLException(
      string message,
      GraphQLRequest request,
      GraphQLResponse<T>? response
    ) : base(message)
    {
      _request = request;
      Response = response;
    }

    public SpeckleGraphQLException(
      string message,
      Exception inner,
      GraphQLRequest request,
      GraphQLResponse<T>? response
    ) : base(message, inner) { }
  }

  public class SpeckleGraphQLException : SpeckleGraphQLException<object>
  {
    public SpeckleGraphQLException(
      string message,
      GraphQLRequest request,
      GraphQLResponse<object>? response
    ) : base(message, request, response) { }
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
