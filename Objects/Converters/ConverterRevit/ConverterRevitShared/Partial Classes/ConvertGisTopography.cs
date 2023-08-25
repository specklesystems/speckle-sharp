using System;
using System.Collections.Generic;
using System.Linq;
using Objects.BuiltElements.Revit;
using Objects.Utils;
using Objects.GIS;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

using Objects.BuiltElements;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationObject GisTopographyToNative(GisTopography gisTopography)
    {
      var speckleTopography = new Objects.BuiltElements.Topography()
      {
        applicationId = gisTopography.applicationId ??= Guid.NewGuid().ToString(),
        displayValue = new List<Geometry.Mesh>()
    };

      foreach (Geometry.Mesh displayMesh in gisTopography.displayValue)
      {
        displayMesh.MeshRemoveDuplicatePts();
        speckleTopography.displayValue.Add(displayMesh);
      }


      return TopographyToNative(speckleTopography);
    }
  }
}
