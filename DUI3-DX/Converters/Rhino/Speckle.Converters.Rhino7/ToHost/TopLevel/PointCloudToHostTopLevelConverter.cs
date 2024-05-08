﻿using Rhino;
using Speckle.Converters.Common;
using Speckle.Converters.Common.Objects;

namespace Speckle.Converters.Rhino7.ToHost.TopLevel;

[NameAndRankValue(nameof(SOG.Pointcloud), NameAndRankValueAttribute.SPECKLE_DEFAULT_RANK)]
public class PointCloudToHostTopLevelConverter : SpeckleToHostGeometryBaseConversion<SOG.Pointcloud, RG.PointCloud>
{
  public PointCloudToHostTopLevelConverter(
    IConversionContextStack<RhinoDoc, UnitSystem> contextStack,
    IRawConversion<SOG.Pointcloud, RG.PointCloud> geometryBaseConverter
  )
    : base(contextStack, geometryBaseConverter) { }
}