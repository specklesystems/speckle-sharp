using System;
using System.Linq;
using System.Collections.Generic;
using GraphQL;
using Sentry.Protocol;

namespace Speckle.Core.Logging
{
  public class SpeckleException : Exception
  {
    public SpeckleException()
    {
    }

    public SpeckleException(string message, SentryLevel level = SentryLevel.Error) : base(message)
    {
      Log.CaptureException(this, level);
    }

    public SpeckleException(string message, Exception inner)
         : base(message, inner)
    {
      Log.CaptureException(this);
    }
  }


  public class GraphQLException : Exception
  {
    public List<KeyValuePair<string, object>> GraphQLErrors { get; set; }
    public GraphQLException()
    {
    }

    public GraphQLException(string message) : base(message)
    {
      Log.CaptureException(this);
    }

    public GraphQLException(string message, GraphQLError[ ] errors) : base(message)
    {
      GraphQLErrors = errors.Select(error => new KeyValuePair<string, object>("error", error.Message)).ToList();
      Log.CaptureException(this, extra:GraphQLErrors);
    }

    public GraphQLException(string message, Exception inner)
         : base(message, inner)
    {
      Log.CaptureException(this);
    }
  }
}
