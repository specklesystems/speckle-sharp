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

  public KitException(string? message)
    : base(message) { }

  public KitException(string? message, Exception? innerException)
    : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when conversion of an object was not successful
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

  public ConversionException(string? message, Exception? innerException)
    : base(message, innerException) { }

  public ConversionException(string? message)
    : base(message) { }

  public ConversionException() { }
}

/// <summary>
/// Exception used when an object could not be converted, because we don't support a specific conversion.
/// </summary>
/// <remarks>
/// This Exception should be thrown as part of a pre-emptive check in conversions (not as part reactive error handling)
/// and usage (throwing) should not be dependent on external state:
/// i.e. given the same object and converter state, the outcome (exception throw or not) should be the same.
/// </remarks>
/// <example>
/// It can be used for:
/// <ul>
///  <li> objects who's <see cref="Type"/> we don't support (e.g. <c>"Walls are not supported"</c>)</li>
///  <li> objects with a property who's value we don't support (e.g. <c>"Beams with shape type of Circular are not supported"</c>)</li>
///  <li> complex object requirements (e.g. <c>"We don't support walls with zero width and no displayValue"</c>)</li>
/// </ul>
/// It should <b>NOT</b> be used for:
/// <ul>
///  <li> Invalid Speckle Objects (e.g. <c>"We don't support walls with null lines"</c>)</li>
///  <li> Objects that we have already converted, and therefore now skip (e.g. <c>"A Wall with the same name was already converted"</c>)</li>
///  <li> Reactive error handling (e.g. "Failed to convert wall, I guess it wasn't supported")</li>
/// </ul>
/// </example>
public class ConversionNotSupportedException : ConversionException
{
  public ConversionNotSupportedException(string? message, object? objectToConvert, Exception? innerException = null)
    : base(message, objectToConvert, innerException) { }

  public ConversionNotSupportedException(string message, Exception innerException)
    : base(message, innerException) { }

  public ConversionNotSupportedException(string message)
    : base(message) { }

  public ConversionNotSupportedException() { }
}

/// <summary>
/// Exception thrown when an object was desirably skipped<br/>
/// </summary>
/// <remarks>
/// <b>Avoid throwing this exception Type!</b><br/>
/// As it introduces some bad patterns for exception handling.
/// <br/>
/// Namely, it encodes how the exception WILL be handled, Not simply what HAS happened.
/// Exceptions shouldn't care how they are handled.
/// <br/>
/// We were also misusing this exception in Revit, to correct for ambiguity in the way certain objects should be traversed,
/// by selectively skipping objects that were already converted by other means.
/// </remarks>
[Obsolete("Avoid using this type. Use " + nameof(ConversionNotSupportedException) + " instead, if appropriate")]
public class ConversionSkippedException : ConversionException
{
  public ConversionSkippedException(string? message, object? objectToConvert, Exception? innerException = null)
    : base(message, objectToConvert, innerException) { }

  public ConversionSkippedException(string message, Exception innerException)
    : base(message, innerException) { }

  public ConversionSkippedException(string message)
    : base(message) { }

  public ConversionSkippedException() { }
}

/// <summary>
/// Exception thrown when an object was not ready to be baked into the document (i.e. the element's host doesn't exist yet)
/// </summary>
public class ConversionNotReadyException : ConversionException
{
  public ConversionNotReadyException(string? message, object? objectToConvert, Exception? innerException = null)
    : base(message, objectToConvert, innerException) { }

  public ConversionNotReadyException(string message, Exception innerException)
    : base(message, innerException) { }

  public ConversionNotReadyException(string message)
    : base(message) { }

  public ConversionNotReadyException() { }
}
