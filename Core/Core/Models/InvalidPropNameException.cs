using System;
using Speckle.Core.Logging;

namespace Speckle.Core.Models;

public class InvalidPropNameException : SpeckleException
{
  public InvalidPropNameException(string propName, string reason)
    : base($"Property '{propName}' is invalid: {reason}") { }

  public InvalidPropNameException() { }

  public InvalidPropNameException(string message)
    : base(message) { }

  public InvalidPropNameException(string message, Exception innerException)
    : base(message, innerException) { }
}
