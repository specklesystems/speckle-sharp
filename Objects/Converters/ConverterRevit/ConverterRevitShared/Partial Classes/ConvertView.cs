using Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;

using DB = Autodesk.Revit.DB;
using View = Objects.BuiltElements.View;
using View3D = Objects.BuiltElements.View3D;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    public View ViewToSpeckle(DB.View revitView)
    {
      switch (revitView.ViewType)
      {
        case ViewType.FloorPlan:
          break;
        case ViewType.CeilingPlan:
          break;
        case ViewType.Elevation:
          break;
        case ViewType.Section:
          break;
        case ViewType.ThreeD:
          break;
        default:
          break;
      }

      var speckleView = new View();

      if (revitView is DB.View3D rv3d)
      {
        //some views have null origin, not sure why, but for now we just ignore them and don't bother the user
        if (rv3d.Origin == null)
          return null;

        speckleView = new View3D
        {
          origin = PointToSpeckle(rv3d.Origin),
          forwardDirection = VectorToSpeckle(rv3d.ViewDirection),
          upDirection = VectorToSpeckle(rv3d.UpDirection),
          isOrthogonal = !rv3d.IsPerspective
        };
      }

      speckleView.name = revitView.Name;


      GetAllRevitParamsAndIds(speckleView, revitView);
      return speckleView;
    }

  }
}