using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL;
using Speckle.Core.Logging;

namespace Speckle.Core.Api;

/// <summary>
/// Base class for GraphQL API exceptions
/// </summary>
public class SpeckleGraphQLException<T> : SpeckleGraphQLException
{
  public new GraphQLResponse<T>? Response => (GraphQLResponse<T>?)base.Response;

  public SpeckleGraphQLException(
    string message,
    GraphQLRequest request,
    GraphQLResponse<T>? response,
    Exception? innerException = null
  )
    : base(message, request, response, innerException) { }

  public SpeckleGraphQLException() { }

  public SpeckleGraphQLException(string? message)
    : base(message) { }

  public SpeckleGraphQLException(string? message, Exception? innerException)
    : base(message, innerException) { }
}

public class SpeckleGraphQLException : SpeckleException
{
  private readonly GraphQLRequest _request;
  public IGraphQLResponse? Response { get; }

  public IEnumerable<string> ErrorMessages =>
    Response?.Errors != null ? Response.Errors.Select(e => e.Message) : Enumerable.Empty<string>();

  public IDictionary<string, object>? Extensions => Response?.Extensions;

  public SpeckleGraphQLException(
    string? message,
    GraphQLRequest request,
    IGraphQLResponse? response,
    Exception? innerException = null
  )
    : base(message, innerException)
  {
    _request = request;
    Response = response;
  }

  public SpeckleGraphQLException() { }

  public SpeckleGraphQLException(string? message)
    : base(message) { }

  public SpeckleGraphQLException(string? message, Exception? innerException)
    : base(message, innerException) { }
}

/// <summary>
/// Represents a "FORBIDDEN" on "UNAUTHORIZED" GraphQL error as an exception.
/// https://www.apollographql.com/docs/apollo-server/v2/data/errors/#unauthenticated
/// https://www.apollographql.com/docs/apollo-server/v2/data/errors/#forbidden
/// </summary>
public class SpeckleGraphQLForbiddenException : SpeckleGraphQLException
{
  public SpeckleGraphQLForbiddenException(
    GraphQLRequest request,
    IGraphQLResponse response,
    Exception? innerException = null
  )
    : base("Your request was forbidden", request, response, innerException) { }

  public SpeckleGraphQLForbiddenException() { }

  public SpeckleGraphQLForbiddenException(string? message)
    : base(message) { }

  public SpeckleGraphQLForbiddenException(string? message, Exception? innerException)
    : base(message, innerException) { }
}

public class SpeckleGraphQLInternalErrorException : SpeckleGraphQLException
{
  public SpeckleGraphQLInternalErrorException(
    GraphQLRequest request,
    IGraphQLResponse response,
    Exception? innerException = null
  )
    : base("Your request failed on the server side", request, response, innerException) { }

  public SpeckleGraphQLInternalErrorException() { }

  public SpeckleGraphQLInternalErrorException(string? message)
    : base(message) { }

  public SpeckleGraphQLInternalErrorException(string? message, Exception? innerException)
    : base(message, innerException) { }
}

public class SpeckleGraphQLStreamNotFoundException : SpeckleGraphQLException
{
  public SpeckleGraphQLStreamNotFoundException(
    GraphQLRequest request,
    IGraphQLResponse response,
    Exception? innerException = null
  )
    : base("Stream not found", request, response, innerException) { }

  public SpeckleGraphQLStreamNotFoundException() { }

  public SpeckleGraphQLStreamNotFoundException(string? message)
    : base(message) { }

  public SpeckleGraphQLStreamNotFoundException(string? message, Exception? innerException)
    : base(message, innerException) { }
}
