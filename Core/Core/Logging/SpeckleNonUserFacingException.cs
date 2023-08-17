#nullable enable
using System;

namespace Speckle.Core.Logging;

/// <summary>
/// These are exceptions who's message is not user friendly
/// </summary>
public class SpeckleNonUserFacingException : SpeckleException
{
  public SpeckleNonUserFacingException() { }

  public SpeckleNonUserFacingException(string? message) : base(message) { }

  public SpeckleNonUserFacingException(string? message, Exception? innerException) : base(message, innerException) { }
}
