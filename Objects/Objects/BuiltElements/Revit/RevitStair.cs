using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;

namespace Objects.BuiltElements.Revit
{
  public class RevitStair : Base
  {
    public string family { get; set; }
    public string type { get; set; }
    public Level level { get; set; }
    public Level topLevel { get; set; }
    public List<Parameter> parameters { get; set; }
    public List<RevitStairPath> runs { get; set; }
    public List<RevitStairPath> landings { get; set; }
    public List<RevitStairPath> supports { get; set; }
    public string elementId { get; set; }


    public RevitStair() { }


  }

  public class RevitStairPath : Base
  {
    public ICurve path { get; set; }
    public List<Parameter> parameters { get; set; }
    public string elementId { get; set; }


    public RevitStairPath() { }
  }



}
