using System;
namespace Speckle.Core.Logging
{
  public class SpeckleException : Exception
  {
    public SpeckleException()
    {
    }

    public SpeckleException(string message) : base(message)
    {
    }

    public SpeckleException(string message, Exception inner)
         : base(message, inner)
    {
    }
  }


  public class GraphQLException : Exception
  {
    public GraphQLException()
    {
    }

    public GraphQLException(string message) : base(message)
    {
    }

    public GraphQLException(string message, Exception inner)
         : base(message, inner)
    {
    }
  }
}
