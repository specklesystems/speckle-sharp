using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Revit
{
  public class ModelCurve : Base
  {
    public ICurve baseCurve { get; set; }
    public string lineStyle { get; set; }
    //public Dictionary<string, object> parameters { get; set; }
  }

  public class DetailCurve : Base
  {
    public ICurve baseCurve { get; set; }
    public string lineStyle { get; set; }
  }

  public class RoomBoundaryLine : Base
  {
    public ICurve baseCurve { get; set; }
    public Dictionary<string, object> parameters { get; set; }
  }
}
