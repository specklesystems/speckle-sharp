#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL;
using Sentry;

namespace Speckle.Core.Logging
{
  public class SpeckleException : Exception
  {
    public List<KeyValuePair<string, object>> GraphQLErrors { get; set; }

    public SpeckleException() { }

    public SpeckleException(string message, Exception? inner = null) : base(message, inner) { }

    [Obsolete("Use any other constructor")]
    public SpeckleException(string message, bool log, SentryLevel level = SentryLevel.Info)
      : base(message)
    {
    }

    public SpeckleException(
      string message,
      GraphQLError[] errors,
      bool log = true,
      SentryLevel level = SentryLevel.Info
    ) : base(message)
    {
      GraphQLErrors = errors
        .Select(error => new KeyValuePair<string, object>("error", error.Message))
        .ToList();
    }

    public SpeckleException(
      string message,
      Exception? inner,
      bool log = true,
      SentryLevel level = SentryLevel.Info
    ) : base(message, inner)
    {
    }
  }
}
