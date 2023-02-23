using System;
using Speckle.Core.Logging;

namespace Speckle.Core.Credentials
{
  public class SpeckleAccountManagerException : SpeckleException
  {
    public SpeckleAccountManagerException(string message) : base(message) { }

    public SpeckleAccountManagerException(string message, Exception inner) : base(message, inner)
    { }
  }

  public class SpeckleAccoundFlowLockedException : SpeckleAccountManagerException
  {
    public SpeckleAccoundFlowLockedException(string message) : base(message) { }
  }
}
