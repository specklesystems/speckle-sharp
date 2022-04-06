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
using Objects.Structural.CSI.Geometry;

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
        
        if (element is Element1D)
        {
          try
          {
            if(element is CSIElement1D){
              var Application = AnalyticalStickToNative((CSIElement1D)element);
              placeholderObjects.Concat(Application);
            }
            else { var Application = AnalyticalStickToNative((Element1D)element);
              placeholderObjects.Concat(Application);
            }


            
          }
          catch { }

        }
        else
        {
          try
          {
            if (element is CSIElement2D)
            {
              var Application = AnalyticalSurfaceToNative((CSIElement2D)element);
              placeholderObjects.Concat(Application);
            }
            else
            {
              var Application = AnalyticalSurfaceToNative((Element2D)element);
              placeholderObjects.Concat(Application);
            }
          }
          catch { }
        }
      }

      return placeholderObjects;
    }

  }
}
