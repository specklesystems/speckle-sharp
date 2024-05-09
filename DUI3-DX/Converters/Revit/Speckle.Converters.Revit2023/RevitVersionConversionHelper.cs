using Speckle.Converters.RevitShared;

namespace Speckle.Converters.Revit2023;

public class RevitVersionConversionHelper : IRevitVersionConversionHelper
{
  public bool IsCurveClosed(DB.NurbSpline nurbsSpline)
  {
    try
    {
      return nurbsSpline.IsClosed;
    }
    catch (Autodesk.Revit.Exceptions.ApplicationException)
    {
      // POC: is this actually a good assumption?
      return true;
    }
  }
}
