using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.DB;

namespace ConverterRevitShared.Extensions
{
  public static class PointExtensions
  {
    public static XYZ ToXYZ(this Objects.Geometry.Point p)
    {
      return new XYZ(p.x, p.y, p.z);
    }
  }
}
