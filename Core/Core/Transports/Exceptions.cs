using System;
using Speckle.Core.Logging;

namespace Speckle.Core.Transports;

public class TransportException : SpeckleException
{
  public ITransport? Transport { get; }

  public TransportException(ITransport? transport, string? message, Exception? innerException = null)
    : this(message, innerException)
  {
    Transport = transport;
  }

  public TransportException() { }

  public TransportException(string? message)
    : base(message) { }

  public TransportException(string? message, Exception? innerException)
    : base(message, innerException) { }
}
