using Speckle.Newtonsoft.Json;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Geometry;


namespace Objects.Structural.Analysis
{
  public class ModelInfo : Base //titles
  {
    public string name { get; set; } //title
    public string description { get; set; } //subtitle
    public string projectNumber { get; set; } //could a project info object be a potential upstream change, as addition to default Speckle Kit?
    public string projectName { get; set; }
    public ModelSettings settings { get; set; }
    public string initials { get; set; } //engineer initials
    public string application { get; set; } //ex. GSA, Tekla (reference Applications class?)     
    public ModelInfo() { }

    /// <summary>
    /// SchemaBuilder constructor for a model specifications (containing general model and project info) object
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="projectNumber"></param>
    /// <param name="projectName"></param>
    /// <param name="settings"></param>
    /// <param name="engInitials">Initials that identify the creator of the model</param>
    /// <param name="application"></param>
    [SchemaInfo("ModelInfo", "Creates a Speckle object which describes basic model and project information for a structural model", "Structural", "Analysis")]
    public ModelInfo(string name = null, string description = null, string projectNumber = null, string projectName = null, ModelSettings settings = null, string engInitials = null, string application = null)
    {
      this.name = name;
      this.description = description;
      this.projectNumber = projectNumber;
      this.projectName = projectName;
      this.settings = settings;
      this.initials = engInitials;
      this.application = application;
    }
  }
}
