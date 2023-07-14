using System.Collections.Generic;
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
  public string display { get; set; }
  
  /// <summary>
  /// Symbol of the unit : eg: m, mm, cm, etc
  /// </summary>
  [DetachProperty]
  public List<Base> symbol { get; set; } 
  
  
}
