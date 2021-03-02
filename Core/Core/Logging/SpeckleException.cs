using System;
using System.Linq;
using System.Collections.Generic;
using GraphQL;
using Sentry.Protocol;

namespace Speckle.Core.Logging
{
  public class SpeckleException : Exception
  {
    public List<KeyValuePair<string, object>> GraphQLErrors { get; set; }
    public SpeckleException()
    {
    }

    public SpeckleException(string message, GraphQLError[ ] errors, bool log = false,
      SentryLevel level = SentryLevel.Info) : base(message)
    {
      GraphQLErrors = errors.Select(error => new KeyValuePair<string, object>("error", error.Message)).ToList();
      if (log)
        Log.CaptureException(this, extra: GraphQLErrors);
    }

    public SpeckleException(string message, bool log = false, SentryLevel level = SentryLevel.Info ) : base(message)
    {
      if (log)
        Log.CaptureException(this, level);
    }

    public SpeckleException(string message, Exception inner, bool log = false, SentryLevel level = SentryLevel.Info)
         : base(message, inner)
    {
      if (log)
        Log.CaptureException(this);
    }
  }
}
