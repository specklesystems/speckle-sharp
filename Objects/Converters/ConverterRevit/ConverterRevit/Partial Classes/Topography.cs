using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Objects.Revit;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public ApplicationPlaceholderObject TopographyToNative(ITopography speckleSurface)
    {
      var docObj = GetExistingElementByApplicationId(speckleSurface.applicationId);

      var pts = new List<XYZ>();
      for (int i = 0; i < speckleSurface.baseGeometry.vertices.Count; i += 3)
      {
        pts.Add(new XYZ(
          ScaleToNative(speckleSurface.baseGeometry.vertices[i], speckleSurface.baseGeometry.units),
          ScaleToNative(speckleSurface.baseGeometry.vertices[i + 1], speckleSurface.baseGeometry.units),
          ScaleToNative(speckleSurface.baseGeometry.vertices[i + 2], speckleSurface.baseGeometry.units)));
      }

      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }

      var revitSurface = TopographySurface.Create(Doc, pts);
      if (speckleSurface is RevitTopography rt)
      {
        SetElementParams(revitSurface, rt);
      }

      return new ApplicationPlaceholderObject { applicationId = speckleSurface.applicationId, ApplicationGeneratedId = revitSurface.UniqueId };
    }

    public RevitTopography TopographyToSpeckle(TopographySurface revitTopo)
    {
      var speckleTopo = new RevitTopography();
      speckleTopo.baseGeometry = GetElementMesh(revitTopo);
      AddCommonRevitProps(speckleTopo, revitTopo);

      return speckleTopo;
    }
  }
}