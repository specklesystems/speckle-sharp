﻿using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.Raw;

public class SpecklePointRawToHostConversion
  : IRawConversion<SOG.Point, RG.Point3d>,
    IRawConversion<SOG.Point, RG.Point>
{
  /// <summary>
  /// Converts a Speckle Point object to a Rhino Point3d object.
  /// </summary>
  /// <param name="target">The Speckle Point object to convert.</param>
  /// <returns>The converted Rhino Point3d object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  public RG.Point3d RawConvert(SOG.Point target) => new(target.x, target.y, target.z);

  /// <summary>
  /// Converts a Speckle Point object to a Rhino Point object.
  /// </summary>
  /// <param name="target">The Speckle Point object to convert.</param>
  /// <returns>The converted Rhino Point object.</returns>
  /// <remarks>⚠️ This conversion does NOT perform scaling.</remarks>
  RG.Point IRawConversion<SOG.Point, RG.Point>.RawConvert(SOG.Point target) => new(RawConvert(target));
}
