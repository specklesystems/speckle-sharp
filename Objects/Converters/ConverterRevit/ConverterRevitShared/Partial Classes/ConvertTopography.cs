using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Objects.Utils;
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
      var facets = new List<PolymeshFacet>();
      foreach (Geometry.Mesh displayMesh in speckleSurface.displayValue)
      {
        // triangulate the mesh first since revit Topography can only accept triangulated meshes
        displayMesh.TriangulateMesh();

        pts.Capacity += displayMesh.vertices.Count / 3;
        for (int i = 0; i < displayMesh.vertices.Count; i += 3)
        {
          var point = new Geometry.Point(displayMesh.vertices[i], displayMesh.vertices[i + 1], displayMesh.vertices[i + 2], displayMesh.units);
          pts.Add(PointToNative(point));
        }

#if REVIT2022 || REVIT2023
        facets.Capacity += (int) (displayMesh.faces.Count / 4f) * 3;
        int j = 0;
        while (j < displayMesh.faces.Count)
        {
          facets.Add(new PolymeshFacet(displayMesh.faces[j + 1], displayMesh.faces[j + 2], displayMesh.faces[j + 3]));
          j += 4;
        }
#endif
      }

      if (docObj != null)
      {
        Doc.Delete(docObj.Id);
      }
      TopographySurface revitSurface = null;
#if REVIT2022 || REVIT2023
      revitSurface = TopographySurface.Create(Doc, pts, facets);
#else
      revitSurface = TopographySurface.Create(Doc, pts);
#endif
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
