using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Geometry;
using Autodesk.Civil.ApplicationServices;
using AC = Autodesk.Civil.DatabaseServices;

using Polycurve = Objects.Geometry.Polycurve;

namespace Objects.Converter.AutoCAD
{
  public partial class ConverterAutoCAD
  {
    // featurelines
    public Polycurve FeatureLineToSpeckle(AC.FeatureLine featureLine)
    {
      return null;
    }
    public AC.FeatureLine FeatureLineToNative(Polycurve polycurve)
    {
      return null;
    }
  }
}
