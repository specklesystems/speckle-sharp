using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Objects.BuiltElements.Revit;
using System.Collections.Generic;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    private RevitTopRail TopRailToSpeckle(TopRail revitTopRail)
    {
      if (!ShouldConvertHostedElement(revitTopRail, Doc.GetElement(revitTopRail.HostRailingId)))
        return null;

      var topRailType = revitTopRail.Document.GetElement(revitTopRail.GetTypeId()) as TopRailType;
      var speckleTopRail = new RevitTopRail
      {
        type = topRailType.Name,
        displayValue = GetElementDisplayMesh(revitTopRail,
                new Options() { DetailLevel = ViewDetailLevel.Fine, ComputeReferences = false })
      };

      GetAllRevitParamsAndIds(speckleTopRail, revitTopRail, new List<string> { });

      Report.Log($"Converted TopRail {revitTopRail.Id}");

      return speckleTopRail;
    }
  }
}
