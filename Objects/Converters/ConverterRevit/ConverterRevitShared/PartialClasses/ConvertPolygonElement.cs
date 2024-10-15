using System;
using System.Collections.Generic;
using System.Linq;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Objects.GIS;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  public ApplicationObject GisFeatureToNative(GisFeature gisFeature)
  {
    // targeting Polygon features with List of Meshes as dispayValue
    var speckleDirectShape = new Objects.BuiltElements.Revit.DirectShape()
    {
      applicationId = gisFeature.applicationId ??= Guid.NewGuid().ToString(),
      baseGeometries = new List<Base>(),
      parameters = new Base(),
      name = "",
      category = RevitCategory.GenericModel
    };

    var traversal = new GraphTraversal(DefaultTraversal.DefaultRule);
    var meshes = traversal.Traverse(gisFeature).Select(tc => tc.current).Where(b => b is Mesh);

    speckleDirectShape.baseGeometries.AddRange(meshes);

    foreach (var kvp in gisFeature.attributes.GetMembers())
    {
      speckleDirectShape.parameters[kvp.Key] = new Objects.BuiltElements.Revit.Parameter()
      {
        name = kvp.Key,
        value = kvp.Value
      };
    }
    return DirectShapeToNative(speckleDirectShape, ToNativeMeshSettingEnum.Default);
  }

  public ApplicationObject PolygonElementToNative(PolygonElement polygonElement)
  {
    var speckleDirectShape = new Objects.BuiltElements.Revit.DirectShape()
    {
      applicationId = polygonElement.applicationId ??= Guid.NewGuid().ToString(),
      baseGeometries = new List<Base>(),
      parameters = new Base(),
      name = "",
      category = RevitCategory.GenericModel
    };

    var traversal = new GraphTraversal(DefaultTraversal.DefaultRule);
    var meshes = traversal.Traverse(polygonElement).Select(tc => tc.current).Where(b => b is Mesh);

    speckleDirectShape.baseGeometries.AddRange(meshes);

    foreach (var kvp in polygonElement.attributes.GetMembers())
    {
      speckleDirectShape.parameters[kvp.Key] = new Objects.BuiltElements.Revit.Parameter()
      {
        name = kvp.Key,
        value = kvp.Value
      };
    }

    return DirectShapeToNative(speckleDirectShape, ToNativeMeshSettingEnum.Default);
  }
}
