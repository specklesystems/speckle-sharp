using System;
using Speckle.Core.Logging;

namespace DUI3.Utils
{
  public class ModelNotFoundException : SpeckleException
  {
    public ModelNotFoundException(string message)
      : base(message) { }

    public ModelNotFoundException(string message, Exception inner)
      : base(message, inner) { }

    public ModelNotFoundException() { }
  }
}
