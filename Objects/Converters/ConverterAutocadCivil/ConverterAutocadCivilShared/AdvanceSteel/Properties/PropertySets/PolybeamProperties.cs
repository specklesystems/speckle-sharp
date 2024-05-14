#if ADVANCESTEEL
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Autodesk.AdvanceSteel.ConstructionTypes;
using Autodesk.AdvanceSteel.Geometry;
using Autodesk.AdvanceSteel.Modelling;
using ASPolyBeam = Autodesk.AdvanceSteel.Modelling.PolyBeam;
using ASPoint3d = Autodesk.AdvanceSteel.Geometry.Point3d;
using System.Linq;

namespace Objects.Converter.AutocadCivil;

public class PolybeamProperties : ASBaseProperties<PolyBeam>, IASProperties
{
  public override Dictionary<string, ASProperty> BuildedPropertyList()
  {
    Dictionary<string, ASProperty> dictionary = new();

    InsertProperty(dictionary, "continuous", nameof(PolyBeam.IsContinuous));

    InsertCustomProperty(dictionary, "points", nameof(PolybeamProperties.GetListPoints), null);
    return dictionary;
  }

  private static List<ASPoint3d> GetListPoints(ASPolyBeam beam)
  {
    var polyLine = beam.GetPolyline(true);
    return polyLine.Vertices.ToList();
  }
}
#endif
