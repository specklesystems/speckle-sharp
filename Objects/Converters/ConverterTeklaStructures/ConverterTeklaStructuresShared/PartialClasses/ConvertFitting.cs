using Objects.Geometry;
using BE = Objects.BuiltElements;
using Tekla.Structures.Model;
using TSG = Tekla.Structures.Geometry3d;

namespace Objects.Converter.TeklaStructures;

public partial class ConverterTeklaStructures
{
  public void FittingToNative(Objects.Geometry.Plane fitting)
  {
    if (fitting is BE.TeklaStructures.Fitting)
    {
      var fit = fitting as BE.TeklaStructures.Fitting;
      Fitting teklaFitting = new();
      teklaFitting.Father = Model.SelectModelObject(new Tekla.Structures.Identifier(fit.hostID));
      teklaFitting.Plane = new Tekla.Structures.Model.Plane();
      teklaFitting.Plane.Origin = new TSG.Point(fit.origin.x, fit.origin.y, fit.origin.z);
      teklaFitting.Plane.AxisX = new TSG.Vector(fit.xdir.x, fit.xdir.y, fit.xdir.z);
      teklaFitting.Plane.AxisY = new TSG.Vector(fit.ydir.x, fit.ydir.y, fit.ydir.z);
      teklaFitting.Insert();
    }
  }

  public BE.TeklaStructures.Fitting FittingsToSpeckle(Fitting fitting)
  {
    var speckleFitting = new BE.TeklaStructures.Fitting();

    var units = GetUnitsFromModel();
    speckleFitting.origin = new Point(fitting.Plane.Origin.X, fitting.Plane.Origin.Y, fitting.Plane.Origin.Z, units);
    speckleFitting.xdir = new Vector(fitting.Plane.AxisX.X, fitting.Plane.AxisX.Y, fitting.Plane.AxisX.Z, units);
    speckleFitting.ydir = new Vector(fitting.Plane.AxisY.X, fitting.Plane.AxisY.Y, fitting.Plane.AxisY.Z, units);
    var normal = fitting.Plane.AxisX.Cross(fitting.Plane.AxisY).GetNormal();
    speckleFitting.normal = new Vector(normal.X, normal.Y, normal.Z, units);
    speckleFitting.hostID = fitting.Father.Identifier.GUID.ToString();
    speckleFitting.units = units;
    speckleFitting.applicationId = fitting.Identifier.GUID.ToString();

    return speckleFitting;
  }
}
