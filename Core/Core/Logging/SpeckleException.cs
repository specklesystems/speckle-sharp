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
    public SpeckleException()
    {
    }

    public SpeckleException(string message, bool log = true, SentryLevel level = SentryLevel.Info) : base(message)
    {
      if (log)
        Log.CaptureException(this, level);
    }

    public SpeckleException(string message, GraphQLError[] errors, bool log = true,
      SentryLevel level = SentryLevel.Info) : base(message)
    {
      GraphQLErrors = errors.Select(error => new KeyValuePair<string, object>("error", error.Message)).ToList();
      if (log)
        Log.CaptureException(this, level, GraphQLErrors);
    }

    public SpeckleException(string message, Exception inner, bool log = true, SentryLevel level = SentryLevel.Info)
         : base(message, inner)
    {
      if (inner is SpeckleException)
        return;
      if (log)
        Log.CaptureException(this, level);
    }
  }
}
