using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Speckle.ConnectorRevit;

internal class CmdAvailabilityViews : IExternalCommandAvailability
{
  /// <summary>
  /// Command Availability - Views
  /// </summary>
  /// <param name="applicationData"></param>
  /// <param name="selectedCategories"></param>
  /// <returns></returns>
  public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
  {
    return applicationData?.ActiveUIDocument?.Document?.IsValidObject ?? false;
  }
}
