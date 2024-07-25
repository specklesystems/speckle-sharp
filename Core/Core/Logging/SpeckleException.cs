using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL;
using Speckle.Core.Api;

namespace Speckle.Core.Logging;

public class SpeckleException : Exception
{
  public SpeckleException() { }

  public SpeckleException(string? message)
    : base(message) { }

  public SpeckleException(string? message, Exception? inner = null)
    : base(message, inner) { }

  #region obsolete
  [Obsolete("Use any other constructor", true)]
  public SpeckleException(string? message, Exception? inner, bool log = true)
    : base(message, inner) { }

  [Obsolete($"Use {nameof(SpeckleGraphQLException)} instead", true)]
  public SpeckleException(string? message, GraphQLError[] errors, bool log = true)
    : base(message)
  {
    GraphQLErrors = errors.Select(error => new KeyValuePair<string, object>("error", error.Message)).ToList();
  }

  [Obsolete("Use any other constructor", true)]
  public SpeckleException(string message, bool log)
    : base(message) { }

  [Obsolete($"Use {nameof(SpeckleGraphQLException)} instead", true)]
  public List<KeyValuePair<string, object>> GraphQLErrors { get; set; }
  #endregion
}
