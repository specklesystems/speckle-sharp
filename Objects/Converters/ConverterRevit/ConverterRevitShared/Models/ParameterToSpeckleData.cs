#nullable enable
using Objects.BuiltElements.Revit;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit.Models
{
  /// <summary>
  /// This struct is used when caching parameter definitions upon sending to avoid having to deep clone the parameter object
  /// This is done because all the fields except the parameter value will change
  /// </summary>
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
