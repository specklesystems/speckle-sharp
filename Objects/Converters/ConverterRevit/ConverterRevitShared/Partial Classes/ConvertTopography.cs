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
    public ApplicationObject TopographyToNative(Topography speckleSurface)
    {
      var docObj = GetExistingElementByApplicationId(((Base)speckleSurface).applicationId);
      var appObj = new ApplicationObject(speckleSurface.id, speckleSurface.speckle_type) { applicationId = speckleSurface.applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj, out appObj))
        return appObj;

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

#if !REVIT2019
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
        Doc.Delete(docObj.Id);

      TopographySurface revitSurface = null;
#if !REVIT2019
      revitSurface = TopographySurface.Create(Doc, pts, facets);
#else
      revitSurface = TopographySurface.Create(Doc, pts);
#endif
      if (speckleSurface is RevitTopography rt)
        SetInstanceParameters(revitSurface, rt);

      appObj.Update(status: ApplicationObject.State.Created, createdId: revitSurface.UniqueId, convertedItem: revitSurface);
      return appObj;
    }

    public RevitTopography TopographyToSpeckle(TopographySurface revitTopo)
    {
      var speckleTopo = new RevitTopography();
      speckleTopo.displayValue = GetElementMesh(revitTopo);
      GetAllRevitParamsAndIds(speckleTopo, revitTopo);
      return speckleTopo;
    }
  }
}
