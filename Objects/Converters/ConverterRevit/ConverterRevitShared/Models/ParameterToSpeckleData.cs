#nullable enable
using Objects.BuiltElements.Revit;

namespace Objects.Converter.Revit.Models
{
  internal struct ParameterToSpeckleData
  {
    public string ApplicationUnits;
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
        //name = definition.Name,
        //applicationInternalName = paramInternalName,
        //isShared = rp.IsShared,
        //isReadOnly = rp.IsReadOnly,
        //isTypeParameter = isTypeParameter,
        //applicationUnitType = definition.GetUnityTypeString(), //eg UT_Length
        //units = GetSymbolUnit(rp),
      };
    }
  }
}
