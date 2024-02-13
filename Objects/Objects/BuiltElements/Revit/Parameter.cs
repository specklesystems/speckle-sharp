using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class Parameter : Base
{
  public Parameter() { }

  [SchemaInfo("Parameter", "A Revit instance parameter to set on an element", "Revit", "Families")]
  public Parameter(
    [SchemaParamInfo("The Revit display name, BuiltInParameter name or GUID (for shared parameters)")] string name,
    object value,
    [SchemaParamInfo(
      "(Optional) Speckle units. If not set it's retrieved from the current document. For non lenght based parameters (eg. Air Flow) it should be set to 'none' so that the Revit display unit will be used instead."
    )]
      string units = ""
  )
  {
    this.name = name;
    this.value = value;
    this.units = units;
    applicationInternalName = name;
  }

  public string name { get; set; }
  public object? value { get; set; }
  public string applicationUnitType { get; set; } //eg UnitType UT_Length
  public string applicationUnit { get; set; } //DisplayUnitType eg DUT_MILLIMITERS
  public string applicationInternalName { get; set; } //BuiltInParameterName or GUID for shared parameter

  /// <summary>
  /// If True it's a Shared Parameter, in which case the ApplicationId field will contain this parameter GUID,
  /// otherwise it will store its BuiltInParameter name
  /// </summary>
  public bool isShared { get; set; }

  public bool isReadOnly { get; set; }

  /// <summary>
  /// True = Type Parameter, False = Instance Parameter
  /// </summary>
  public bool isTypeParameter { get; set; }

  public string units { get; set; }
}
