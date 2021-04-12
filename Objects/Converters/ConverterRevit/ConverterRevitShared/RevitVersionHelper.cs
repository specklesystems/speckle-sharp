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
#if !(REVIT2021)
      return curve.isClosed;
#else
      // dynamo for revit also uses this converter
      // but it default to the 2021 version, so if this method is called 
      // by an earlier version it might throw
      try
      {
        return curve.IsClosed;
      }
      catch
      {
        return true;
      }
#endif
    }

    public static bool IsCurveClosed(Curve curve)
    {
#if !(REVIT2021)
      if (curve.IsBound && curve.GetEndPoint(0).IsAlmostEqualTo(curve.GetEndPoint(1)))
        return true;
      else if (!curve.IsBound && curve.IsCyclic)
        return true;
      return false;
#else
      // dynamo for revit also uses this converter
      // but it default to the 2021 version, so if this method is called 
      // by an earlier version it might throw
      try
      {
        return curve.IsClosed;
      }
      catch
      {
        return true;
      }
#endif
    }
  }
}
