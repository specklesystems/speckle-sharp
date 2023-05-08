#nullable enable
using System;
using Speckle.Core.Logging;

namespace Speckle.Core.Kits;

/// <summary>
/// Exception thrown when an <see cref="ISpeckleKit"/> fails to load/initialise
/// </summary>
/// <remarks>
/// Does NOT inherit from <see cref="SpeckleException"/>, because this usage of this exception is not dependent on Speckle Data (user data)
/// Ideally, this exception should contain a meaningful message, and a reference to the <see cref="ISpeckleKit"/>
/// </remarks>
public class KitException : Exception
{
  /// <summary>
  /// A reference to the <see cref="ISpeckleKit"/> that failed to perform
  /// </summary>
  public ISpeckleKit? Kit { get; }

  public KitException(string? message, ISpeckleKit? kit, Exception? innerException = null)
    : base(message, innerException)
  {
    Kit = kit;
  }

  public KitException() { }

  public KitException(string? message) : base(message) { }

  public KitException(string? message, Exception? innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when conversion of an object fails
/// </summary>
/// <remarks>
/// Ideally this exception contains a meaningful message, and reference to the object that failed to be converted.
/// This exception can be used for both ToSpeckle and ToNative conversion
/// </remarks>
public class ConversionException : SpeckleException
{
  private object? ObjectThatFailed { get; }

  public ConversionException(string? message, object? objectToConvert, Exception? innerException = null)
    : base(message, innerException)
  {
    this.ObjectThatFailed = objectToConvert;
  }

  public ConversionException(string? message, Exception? innerException) : base(message, innerException) { }

  public ConversionException(string? message) : base(message) { }

  public ConversionException() { }
}

/// <summary>
/// Exception thrown when an object was desirably skipped
/// </summary>
public class ConversionSkippedException : ConversionException
{
  public ConversionSkippedException(string? message, object? objectToConvert, Exception? innerException = null)
    : base(message, objectToConvert, innerException) { }

  public ConversionSkippedException(string message, Exception innerException) : base(message, innerException) { }

  public ConversionSkippedException(string message) : base(message) { }

  public ConversionSkippedException() { }
}
