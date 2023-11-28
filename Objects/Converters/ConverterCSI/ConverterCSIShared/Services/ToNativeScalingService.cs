#nullable enable
using System;
using CSiAPIv1;
using Speckle.Core.Kits;

namespace ConverterCSIShared.Services;

public class ToNativeScalingService
{
  public ToNativeScalingService(cSapModel cSapModel)
  {
    var unitsArray = cSapModel.GetPresentUnits().ToString().Split('_');

    ForceUnits = unitsArray[0];
    LengthUnits = unitsArray[1];
    TempuratureUnits = unitsArray[2];
  }

  public string LengthUnits { get; private set; }
  public string ForceUnits { get; private set; }
  public string TempuratureUnits { get; private set; }

  /// <summary>
  /// Scales a value from a length unit of a specified power to the native length unit to the same power
  /// </summary>
  /// <param name="value"></param>
  /// <param name="speckleUnits"></param>
  /// <param name="power"></param>
  /// <returns></returns>
  public double ScaleLength(double value, string speckleUnits, int power = 1)
  {
    return value * Math.Pow(Units.GetConversionFactor(speckleUnits, LengthUnits), power);
  }
}
