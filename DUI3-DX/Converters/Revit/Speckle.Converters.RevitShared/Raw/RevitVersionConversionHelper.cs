using System.Diagnostics.CodeAnalysis;
using Speckle.InterfaceGenerator;
using Speckle.Revit.Interfaces;

namespace Speckle.Converters.Revit2023;

[GenerateAutoInterface]
public class RevitVersionConversionHelper : IRevitVersionConversionHelper
{
  [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
  public bool IsCurveClosed(IRevitNurbSpline nurbsSpline)
  {
    try
    {
      return nurbsSpline.IsClosed;
    }
    catch (Exception)
    {
      // POC: is this actually a good assumption?
      return true;
    }
  }
}
