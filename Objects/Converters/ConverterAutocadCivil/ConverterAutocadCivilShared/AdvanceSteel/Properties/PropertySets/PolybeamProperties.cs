#if ADVANCESTEEL2023
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Autodesk.AdvanceSteel.ConstructionTypes;
using Autodesk.AdvanceSteel.Geometry;
using Autodesk.AdvanceSteel.Modelling;

namespace Objects.Converter.AutocadCivil
{
  public class PolybeamProperties : ASBaseProperties<PolyBeam>, IASProperties
  {
    public override Dictionary<string, ASProperty> BuildedPropertyList()
    {
      Dictionary<string, ASProperty> dictionary = new Dictionary<string, ASProperty>();

      InsertProperty(dictionary, "continuous", nameof(PolyBeam.IsContinuous));

      return dictionary;
    }

  }
}
#endif
