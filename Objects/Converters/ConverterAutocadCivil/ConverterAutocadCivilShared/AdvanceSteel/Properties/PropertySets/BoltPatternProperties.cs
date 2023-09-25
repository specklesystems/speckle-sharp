#if ADVANCESTEEL
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Autodesk.AdvanceSteel.ConstructionTypes;
using Autodesk.AdvanceSteel.Geometry;
using Autodesk.AdvanceSteel.Modelling;
using ASPoint3d = Autodesk.AdvanceSteel.Geometry.Point3d;
using System.Linq;

namespace Objects.Converter.AutocadCivil;

public class BoltPatternProperties : ASBaseProperties<BoltPattern>, IASProperties
{
  public override Dictionary<string, ASProperty> BuildedPropertyList()
  {
    Dictionary<string, ASProperty> dictionary = new Dictionary<string, ASProperty>();

    InsertProperty(dictionary, "reference point", nameof(BoltPattern.RefPoint));
    InsertProperty(dictionary, "number of screws", nameof(BoltPattern.NumberOfScrews));
    InsertProperty(dictionary, "is inverted", nameof(BoltPattern.IsInverted));
    InsertProperty(dictionary, "center", nameof(BoltPattern.Center));
    InsertProperty(dictionary, "x direction", nameof(BoltPattern.XDirection));
    InsertProperty(dictionary, "bolt normal", nameof(BoltPattern.BoltNormal));
    InsertProperty(dictionary, "normal", nameof(BoltPattern.Normal));
    InsertProperty(dictionary, "y direction", nameof(BoltPattern.YDirection));

    InsertCustomProperty(dictionary, "middle points", nameof(BoltPatternProperties.GetMidPoints), null);

    return dictionary;
  }
  private static IEnumerable<ASPoint3d> GetMidPoints(BoltPattern boltPattern)
  {
    boltPattern.GetMidpoints(out var points);
    return points;
  }
}
#endif
