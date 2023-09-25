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
using Autodesk.AdvanceSteel.CADAccess;

namespace Objects.Converter.AutocadCivil;

public class FilerObjectProperties : ASBaseProperties<FilerObject>, IASProperties
{
  public override Dictionary<string, ASProperty> BuildedPropertyList()
  {
    Dictionary<string, ASProperty> dictionary = new Dictionary<string, ASProperty>();

    InsertProperty(dictionary, "layer", nameof(FilerObject.Layer));
    InsertProperty(dictionary, "handle", nameof(FilerObject.Handle));
    InsertProperty(dictionary, "type", nameof(FilerObject.Type));

    return dictionary;
  }
}
#endif
