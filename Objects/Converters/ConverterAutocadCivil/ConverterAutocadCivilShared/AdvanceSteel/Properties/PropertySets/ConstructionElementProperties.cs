#if ADVANCESTEEL2023
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

public class ConstructionElementProperties : ASBaseProperties<ConstructionElement>, IASProperties
{
  public override Dictionary<string, ASProperty> BuildedPropertyList()
  {
    Dictionary<string, ASProperty> dictionary = new Dictionary<string, ASProperty>();

    InsertProperty(dictionary, "role description", nameof(ConstructionElement.RoleDescription));
    InsertProperty(dictionary, "pure role", nameof(ConstructionElement.PureRole));
    InsertProperty(dictionary, "center point", nameof(ConstructionElement.CenterPoint));
    InsertProperty(dictionary, "role", nameof(ConstructionElement.Role));
    InsertProperty(dictionary, "display mode", nameof(ConstructionElement.ReprMode));

    return dictionary;
  }
}
#endif
