namespace Speckle.Converters.Common;

public class SpeckleConversionException : Exception
{
  public SpeckleConversionException() { }

  public SpeckleConversionException(string message)
    : base(message) { }

  public SpeckleConversionException(string message, Exception innerException)
    : base(message, innerException) { }
}
