using System;
using System.Drawing;
using Objects.Other;
using Rhino;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Models;
#if GRASSHOPPER
#endif

namespace Objects.Converter.RhinoGh;

public partial class ConverterRhinoGh
{
  // doc, aka base commit
  public Collection CollectionToSpeckle(RhinoDoc doc)
  {
    return new Collection("Rhino Model", "rhino model");
  }

  // layers
  public ApplicationObject CollectionToNative(Collection collection)
  {
    var appObj = new ApplicationObject(collection.id, collection.speckle_type)
    {
      applicationId = collection.applicationId
    };
    Layer layer = null;
    var status = ApplicationObject.State.Unknown;

    if (collection["path"] is string path)
    {
      string layerPath = MakeValidPath(path);

      // see if this layer already exists in the doc
      Layer existingLayer = GetLayer(Doc, layerPath);

      // update this layer if it exists & receive mode is on update
      if (existingLayer != null && ReceiveMode == ReceiveMode.Update)
      {
        layer = existingLayer;
        status = ApplicationObject.State.Updated;
      }
      else // create this layer
      {
        if (GetLayer(Doc, layerPath, true) is Layer newLayer)
        {
          layer = newLayer;
          status = ApplicationObject.State.Created;
        }
      }

      if (layer == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Could not create layer with path {layerPath}");
        return appObj;
      }

      // get attributes and rendermaterial
      var displayStyle =
        collection["displayStyle"] as DisplayStyle != null
          ? DisplayStyleToNative(collection["displayStyle"] as DisplayStyle)
          : new ObjectAttributes { ObjectColor = Color.AliceBlue };
      var renderMaterial =
        collection["renderMaterial"] as RenderMaterial != null
          ? RenderMaterialToNative(collection["renderMaterial"] as RenderMaterial)
          : null;
      layer.Color = displayStyle.ObjectColor;
      if (renderMaterial != null)
      {
        layer.RenderMaterial = renderMaterial;
      }

      layer.PlotWeight = displayStyle.PlotWeight;
      layer.LinetypeIndex = displayStyle.LinetypeIndex;

      appObj.Update(status: status, convertedItem: layer, createdId: layer.Id.ToString());
      return appObj;
    }
    else
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Collection didn't have a layer path");
      return appObj;
    }
  }

  public Collection LayerToSpeckle(Layer layer)
  {
    var collection = new Collection(layer.Name, "layer") { applicationId = layer.Id.ToString() };

    // add dynamic rhino props
    collection["visible"] = layer.IsVisible;

    return collection;
  }
}
