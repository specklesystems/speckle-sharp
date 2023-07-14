using Speckle.Core.Kits;
using Speckle.Core.Models;

namespace Objects.BuiltElements.Revit;

public class Units : Base
{
  [SchemaInfo("Units", "A Revit instance units of revit parameter", "Revit","ForgeTypeId")]
  public Units()
  {
  }
  /// <summary>
  ///  Display name of the unit : eg: Meter, Millimeter, Centimeter, etc
  /// </summary>
  public string Display { get; set; }
  
  /// <summary>
  /// Symbol of the unit : eg: m, mm, cm, etc
  /// </summary>
  public string Symbol { get; set; } 
  
}
