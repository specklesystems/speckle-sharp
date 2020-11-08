using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Revit
{
  public interface IRevit
  {
    string elementId { get; set; }
    string applicationId { get; set; }

    Dictionary<string, object> parameters { get; set; }

  }
  public interface IRevitElement : IRevit
  {
    string type { get; set; }
    string family { get; set; } // this can be null
    RevitLevel level { get; set; } // this can be null

    Dictionary<string, object> typeParameters { get; set; }

  }





}
