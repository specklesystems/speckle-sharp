using System.Collections.Generic;
using Autodesk.Revit.DB.Architecture;
using Objects.BuiltElements.Revit;

namespace Objects.Converter.Revit;

public partial class ConverterRevit
{
  private RevitTopRail TopRailToSpeckle(TopRail revitTopRail)
  {
    var topRailType = revitTopRail.Document.GetElement(revitTopRail.GetTypeId()) as TopRailType;
    var speckleTopRail = new RevitTopRail
    {
      type = topRailType.Name,
      displayValue = GetElementDisplayValue(revitTopRail)
    };

    GetAllRevitParamsAndIds(speckleTopRail, revitTopRail, new List<string> { });

    Report.Log($"Converted TopRail {revitTopRail.Id}");

    return speckleTopRail;
  }
}
