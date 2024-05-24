namespace Speckle.Connectors.DUI.Exceptions;

public class SpeckleSendFilterException : Exception
{
  public SpeckleSendFilterException() { }

  public SpeckleSendFilterException(string message)
    : base(message) { }

  public SpeckleSendFilterException(string message, Exception innerException)
    : base(message, innerException) { }
}
