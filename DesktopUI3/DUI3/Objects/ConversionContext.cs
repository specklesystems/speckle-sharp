using Speckle.Core.Models;

namespace DUI3.Objects;

// TODO: This should be move to the core?
// What we pass into conversion
// Info that the converter needs to convert an object
public class ConversionContext //Name pending
{
  public Base ToConvert { get; set; }
  public string ParentApplicationId { get; set; } //Rhino, we interpret this as layer path, in other connectors we interpret this as parent object id
  // TODO: public List<double> LocalToGlobalTransformation { get; set; } // For connectors which don't have blocks

  //public string LayerPath { get; set; } //What rhino needs <-- Not actually rhino specific, speckle collections paths, we can create this always
  //public ElementId hostElementId { get; set; } //What Revit needs
  //public UnityObject parentObject { get; set; } //Reference to the parent object
}
