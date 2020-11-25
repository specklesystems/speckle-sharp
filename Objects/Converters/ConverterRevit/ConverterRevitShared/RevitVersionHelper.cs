using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Converter.Revit
{
  public static class RevitVersionHelper
  {
    public static bool IsCurveClosed(NurbSpline curve)
    {
#if REVIT2021
      return curve.IsClosed;
# else
      return curve.isClosed;
#endif
    }

    public static bool IsCurveClosed(Curve curve)
    {
#if REVIT2021
      return curve.IsClosed;
#else
      if (curve.IsBound && curve.GetEndPoint(0).IsAlmostEqualTo(curve.GetEndPoint(1)))
        return true;
      else if (!curve.IsBound && curve.IsCyclic)
        return true;
      return false;
#endif
    }
  }
}
