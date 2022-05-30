using System.Collections.Generic;
using Speckle.Core.Models;

namespace Objects.Organization
{
  /// <summary>
  /// A basic container object representing a 3D model.
  /// This could be used as a root commit object 
  /// </summary>
  public class Model : Base
  {
    /// <summary>
    /// [Optional] A <see cref="ModelInfo"/> object containing metadata about this <see cref="Model"/>
    /// </summary>
    public ModelInfo info { get; set; }
    
    /// <summary>
    /// [Optional] A <see cref="ModelSettings"/> object containing any specific settings for sending / receiving this data.
    /// </summary>
    public ModelSettings settings { get; set; }
    
    // TODO: do we want a defined field here for the objects or should you be able to add as many "layers" as you want
    // like we currently do? Leaning towards the latter, but then we also prob want a "Layer" / "Container" object
  }

  public class ModelInfo : Base
  {
    public string name { get; set; }
    public string description { get; set; }
  }

  public class ModelSettings : Base
  {
    
  }
}