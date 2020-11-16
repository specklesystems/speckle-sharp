using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Text;

namespace Objects.Revit
{
  /// <summary>
  /// Interface for all the Object kit classes specific to Revit
  /// </summary>
  public interface IRevit
  {
    string elementId { get; set; }
    Dictionary<string, object> parameters { get; set; }
  }
}
