using System;
using System.Collections.Generic;
using System.Linq;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Objects.GIS;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject GisTopographyToNative(GisTopography gisTopography)
    {
      var speckleTopography = new Objects.BuiltElements.Topography()
      {
        applicationId = gisTopography.applicationId ??= Guid.NewGuid().ToString(),
        displayValue = gisTopography.displayValue
      };

      return TopographyToNative(speckleTopography);
    }
  }
}
