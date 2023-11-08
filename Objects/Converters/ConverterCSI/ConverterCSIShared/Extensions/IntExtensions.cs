#nullable enable
using System;
using Speckle.Core.Kits;

namespace ConverterCSIShared.Extensions
{
  internal static class IntExtensions
  {
    public static bool IsSuccessful(this int success)
    {
      return success == 0;
    }
    public static void ThrowIfUnsuccessful(this int success, string message)
    { 
      if (success == 0)
      {
        return;
      }

      throw new ConversionException(message);
    }
    public static void ThrowIfUnsuccessful<T>(this int success, string message)
      where T : Exception
    { 
      if (success == 0)
      {
        return;
      }

      throw (T)Activator.CreateInstance(typeof(T), message);
    }
  }
}
