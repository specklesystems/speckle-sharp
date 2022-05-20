using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Objects.Other
{
  /// <summary>
  /// Used to store application-specific properties
  /// </summary>
  public class ApplicationProps : Base
  {
    public string id { get; set; }
    public string app { get; set; }
    public string className { get;set;}
    public Dictionary<string, object> props { get; set; } = new Dictionary<string, object>();

    public ApplicationProps() { }
  }
}
