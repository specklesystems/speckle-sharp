using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Speckle.Newtonsoft.Json;

namespace Objects.BuiltElements
{
  public class Structure : Base, IDisplayMesh, IDisplayValue<List<Mesh>>
  {
    public Point location { get; set; }
    public List<string> pipeIds { get; set; }
    
    [DetachProperty]
    public List<Mesh> displayValue { get; set; }
    
    public string units { get; set; }

    public Structure() { }
    
    #region Obsolete Members
    [JsonIgnore, Obsolete("Use " + nameof(displayValue) + " instead")]
    public Mesh displayMesh {
      get => displayValue?.FirstOrDefault();
      set => displayValue = new List<Mesh> {value};
    }
    #endregion
  }
}
