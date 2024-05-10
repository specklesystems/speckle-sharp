// POC: why is SpeckleException in this namespace? :8

using Speckle.Core.Logging;

namespace Speckle.Connectors.DUI.Utils;

public class ModelNotFoundException : SpeckleException
{
  public ModelNotFoundException(string message)
    : base(message) { }

  public ModelNotFoundException(string message, Exception inner)
    : base(message, inner) { }

  public ModelNotFoundException() { }
}
