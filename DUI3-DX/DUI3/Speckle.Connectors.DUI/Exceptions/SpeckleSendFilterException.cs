using Speckle.Core.Logging;

namespace Speckle.Connectors.DUI.Exceptions;

public class SpeckleSendFilterException : SpeckleException
{
  public SpeckleSendFilterException() { }

  public SpeckleSendFilterException(string message)
    : base(message) { }

  public SpeckleSendFilterException(string message, Exception innerException)
    : base(message, innerException) { }
}
