using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Rhino.Geometry;
using Rhino.Geometry.Collections;
using RH = Rhino.DocObjects;
using Grasshopper.Kernel.Types;

using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;

using Objects.Geometry;
using Objects.Primitive;
using Objects.Utils;
using Collection = Objects.Organization.Collection;
using Rhino.DocObjects;

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
      var renderMaterial = RenderMaterialToSpeckle(layer.RenderMaterial.SimulateMaterial(true));
      if (renderMaterial != null) collection["renderMaterial"] = renderMaterial;
      var displayStyle = DisplayStyleToSpeckle(new ObjectAttributes(), layer);
      if (displayStyle != null) collection["displayStyle"] = displayStyle;

      return collection;
    }

  }
}
