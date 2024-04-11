using System.Collections.Generic;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace ConnectorRhinoWebUI.Extensions;

public static class BoundingBoxExtensions
{
  /// <summary>
  /// Calculate bounding box with given rhino objects.
  /// </summary>
  /// <param name="rhinoObjects"> RhinoObjects to calculate union of bounding box.</param>
  /// <returns></returns>
  public static BoundingBox UnionRhinoObjects(IEnumerable<RhinoObject> rhinoObjects)
  {
    BoundingBox boundingBox = BoundingBox.Unset;
    foreach (RhinoObject obj in rhinoObjects)
    {
      BoundingBox objBoundingBox = obj.Geometry.GetBoundingBox(false);
      if (objBoundingBox.IsValid)
      {
        if (boundingBox.IsValid)
        {
          boundingBox.Union(objBoundingBox);
        }
        else
        {
          boundingBox = objBoundingBox;
        }
      }
    }

    return boundingBox;
  }
}
