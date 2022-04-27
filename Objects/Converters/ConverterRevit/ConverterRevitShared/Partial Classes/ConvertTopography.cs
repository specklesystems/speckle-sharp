using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationPlaceholderObject TopographyToNative(Topography speckleSurface)
    {
      var docObj = GetExistingElementByApplicationId(((Base)speckleSurface).applicationId);

      if (docObj != null && ReceiveMode == Speckle.Core.Kits.ReceiveMode.Ignore)
        return new ApplicationPlaceholderObject { applicationId = speckleSurface.applicationId, ApplicationGeneratedId = docObj.UniqueId, NativeObject = docObj };

      var pts = new List<XYZ>();
      foreach (Geometry.Mesh displayMesh in speckleSurface.displayValue)
      {
        pts.Capacity += displayMesh.vertices.Count / 3;
        for (int i = 0; i < displayMesh.vertices.Count; i += 3)
        {
          var point = new Geometry.Point(displayMesh.vertices[i], displayMesh.vertices[i + 1], displayMesh.vertices[i + 2], displayMesh.units);
          pts.Add(PointToNative(point));
        }
      }

      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      var revitSurface = TopographySurface.Create(Doc, pts);
      if (speckleSurface is RevitTopography rt)
      {
        SetInstanceParameters(revitSurface, rt);
      }
      Report.Log($"Created Topography {revitSurface.Id}");
      return new ApplicationPlaceholderObject { applicationId = ((Base)speckleSurface).applicationId, ApplicationGeneratedId = revitSurface.UniqueId, NativeObject = revitSurface };
    }

    public RevitTopography TopographyToSpeckle(TopographySurface revitTopo)
    {
      var speckleTopo = new RevitTopography();
      speckleTopo.displayValue = GetElementMesh(revitTopo);
      GetAllRevitParamsAndIds(speckleTopo, revitTopo);
      Report.Log($"Converted Topography {revitTopo.Id}");
      return speckleTopo;
    }
  }
}