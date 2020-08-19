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
}
