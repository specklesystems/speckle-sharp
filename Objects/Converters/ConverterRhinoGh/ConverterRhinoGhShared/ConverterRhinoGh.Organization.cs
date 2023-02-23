using System;

using Rhino.DocObjects;
using Grasshopper.Kernel.Types;

using Collection = Objects.Organization.Collection;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh
  {

    // layers
    public Layer CollectionToNative(Collection collection)
    {
      return null;
    }

    public Collection LayerToSpeckle(Layer layer)
    {
      var collection = new Collection(layer.Name, "layer") { applicationId = layer.Id.ToString() };

      // add dynamic rhino props
      collection["path"] = layer.FullPath;
      collection["visible"] = layer.IsVisible;

      return collection;
    }

  }
}
