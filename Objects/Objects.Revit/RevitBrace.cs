using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;

namespace Objects.Revit
{
  public class RevitBrace : Brace, IRevitElement
  {
    public string family { get; set; }
    public string type { get; set; }
    public Dictionary<string, object> parameters { get; set; }
    public Dictionary<string, object> typeParameters { get; set; }
    public string elementId { get; set; }
    public RevitLevel level { get; set; }


  }
}
