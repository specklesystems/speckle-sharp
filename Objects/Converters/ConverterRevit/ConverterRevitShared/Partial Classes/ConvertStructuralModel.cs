using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Objects.Structural.Geometry;
using Vector = Objects.Geometry.Vector;
using Plane = Objects.Geometry.Plane;
using Speckle.Core.Models;
using DB = Autodesk.Revit.DB;
using Objects.Structural.Analysis;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<ApplicationPlaceholderObject> StructuralModelToNative(Model speckleStructModel)
    {
      List<ApplicationPlaceholderObject> placeholderObjects = new List<ApplicationPlaceholderObject> { };
      foreach (Node node in speckleStructModel.nodes)
      {
        var Application = AnalyticalNodeToNative(node);
        placeholderObjects.Concat(Application);
      }
      foreach (var element in speckleStructModel.elements)
      {
        Element1D element1D = new Element1D();
        if (element.GetType().Equals(element1D.GetType()))
        {
          try
          {
            var Application = AnalyticalStickToNative((Element1D)element);
            placeholderObjects.Concat(Application);
          }
          catch { }

        }
        else
        {
          try
          {
            var Application = AnalyticalSurfaceToNative((Element2D)element);
            placeholderObjects.Concat(Application);
          }
          catch { }
        }
      }

      return placeholderObjects;
    }

  }
}
