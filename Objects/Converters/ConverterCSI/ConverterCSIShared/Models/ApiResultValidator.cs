using System;
using Speckle.Core.Kits;

namespace ConverterCSIShared.Models;

public static class ApiResultValidator
{
  public static bool IsSuccessful(int success)
  {
    return success == 0;
  }

  public static void ThrowIfUnsuccessful(int success, string message)
  {
    if (IsSuccessful(success))
    {
      return;
    }

    throw new ConversionException(message);
  }

  public static void ThrowIfUnsuccessful<T>(int success, string message)
    where T : Exception
  {
    if (IsSuccessful(success))
    {
      return;
    }

    throw (T)Activator.CreateInstance(typeof(T), message);
  }
}
