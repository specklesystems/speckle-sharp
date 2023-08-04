#nullable enable
using System;

namespace Speckle.Core.Transports;

public class TransportException : Exception
{
  public ITransport? Transport { get; }

  public TransportException(ITransport? transport, string? message, Exception? innerException = null)
    : base(message, innerException)
  {
    Transport = transport;
  }

  public TransportException() { }

  public TransportException(string message) : base(message) { }

  public TransportException(string message, Exception innerException) : base(message, innerException) { }
}
