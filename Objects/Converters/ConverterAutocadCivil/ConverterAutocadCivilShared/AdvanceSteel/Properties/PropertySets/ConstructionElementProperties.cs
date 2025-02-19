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

public class ConstructionElementProperties : ASBaseProperties<ConstructionElement>, IASProperties
{
  public override Dictionary<string, ASProperty> BuildedPropertyList()
  {
    Dictionary<string, ASProperty> dictionary = new();

    InsertProperty(dictionary, "role description", nameof(ConstructionElement.RoleDescription));
    InsertProperty(dictionary, "pure role", nameof(ConstructionElement.PureRole));
    InsertProperty(dictionary, "center point", nameof(ConstructionElement.CenterPoint));
    InsertProperty(dictionary, "role", nameof(ConstructionElement.Role));
    InsertProperty(dictionary, "display mode", nameof(ConstructionElement.ReprMode));

    //ActiveConstructionElement has only 1 property, we put together here in ConstructionElementProperties
    InsertCustomProperty(
      dictionary,
      "driven connection",
      nameof(ConstructionElementProperties.GetNumberOfDrivenConObj),
      null
    );

    return dictionary;
  }

  private static double GetNumberOfDrivenConObj(ConstructionElement constructionElement)
  {
    if (constructionElement is ActiveConstructionElement activeConstructionElement)
    {
      return activeConstructionElement.NumberOfDrivenConObj;
    }
    else
    {
      return 0;
    }
  }
}
#endif
