#nullable enable
using Objects.BuiltElements.Revit;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit.Models
{
  internal struct ParameterToSpeckleData
  {
    public string ApplicationUnits;
    public DB.Definition Definition;
    public string InternalName;
    public bool IsReadOnly;
    public bool IsShared;
    public bool IsTypeParameter;
    public string Name;
    public string UnitsSymbol;
    public string UnitType;

    public Parameter GetParameterObjectWithValue(object? value)
    {
      return new Parameter()
      {
        applicationInternalName = InternalName,
        applicationUnit = ApplicationUnits,
        isShared = IsShared,
        isReadOnly = IsReadOnly,
        isTypeParameter = IsTypeParameter,
        name = Name,
        units = UnitsSymbol,
        value = value
      };
    }
  }
}
