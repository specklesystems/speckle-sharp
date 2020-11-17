using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Objects.BuiltElements;
using Objects.Revit;
using System.Collections.Generic;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public TopographySurface TopographyToNative(Topography speckleSurface)
    {
      var (docObj, stateObj) = GetExistingElementByApplicationId(speckleSurface.applicationId, speckleSurface.speckle_type);

      var pts = new List<XYZ>();
      for (int i = 0; i < speckleSurface.baseGeometry.vertices.Count; i += 3)
      {
        pts.Add(new XYZ(
          speckleSurface.baseGeometry.vertices[i] * Scale,
          speckleSurface.baseGeometry.vertices[i + 1] * Scale,
          speckleSurface.baseGeometry.vertices[i + 2] * Scale));
      }

      if (docObj != null)
      {
        Doc.Delete(docObj.Id);

        // TODO: Can't start a transaction here as we have started a global transaction for the creation of all objects.
        // TODO: Let each individual ToNative method handle its own transactions. It's a big change, so will leave for later.

        //var srf = (TopographySurface) docObj;

        //using( TopographyEditScope e = new TopographyEditScope( Doc, "Speckle Topo Edit" ) )
        //{
        //  e.Start(srf.Id);
        //  srf.DeletePoints( srf.GetPoints() );
        //  srf.AddPoints( pts );
        //  e.Commit( null );
        //}
        //return srf;
      }

      var revitSurface = TopographySurface.Create(Doc, pts);
      if (speckleSurface is IRevitElement ire)
        SetElementParams(revitSurface, ire);

      return revitSurface;
    }

    public RevitTopography TopographyToSpeckle(TopographySurface revitTopo)
    {
      var speckleTopo = new RevitTopography();
      speckleTopo.baseGeometry = MeshUtils.GetElementMesh(revitTopo, Scale);
      AddCommonRevitProps(speckleTopo, revitTopo);

      return speckleTopo;
    }
  }
}