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
    #region local functions
    Layer GetLayer(string path)
    {
      var index = Doc.Layers.FindByFullPath(path, RhinoMath.UnsetIntIndex);
      if (index != RhinoMath.UnsetIntIndex)
      {
        return Doc.Layers[index];
      }

      return null;
    }
    Layer MakeLayer(string name, Layer parentLayer = null)
    {
      Layer newLayer = new() { Name = name };
      if (parentLayer != null)
      {
        newLayer.ParentLayerId = parentLayer.Id;
      }

      int newIndex = Doc.Layers.Add(newLayer);
      if (newIndex < 0)
      {
        return null;
      }

      return Doc.Layers.FindIndex(newIndex);
    }
    #endregion

    var appObj = new ApplicationObject(collection.id, collection.speckle_type)
    {
      applicationId = collection.applicationId
    };

    // see if this layer already exists in the doc
    var layerPath = MakeValidPath(collection["path"] as string);
    Layer existingLayer = GetLayer(layerPath);

    // update this layer if it exists & receive mode is on update
    Layer layer;
    ApplicationObject.State status;
    if (existingLayer != null && ReceiveMode == ReceiveMode.Update)
    {
      layer = existingLayer;
      status = ApplicationObject.State.Updated;
    }
    else // create this layer
    {
      Layer parent = null;
      var parentIndex = layerPath.LastIndexOf(Layer.PathSeparator);
      if (parentIndex != -1)
      {
        var parentPath = layerPath.Substring(0, parentIndex);
        parent = GetLayer(parentPath);
        if (parent == null)
        {
          appObj.Update(status: ApplicationObject.State.Failed, logItem: $"Could not find layer parent: {parentPath}");
          return appObj;
        }
      }
      layer = MakeLayer(collection.name, parent);
      status = ApplicationObject.State.Created;
    }

    if (layer == null)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Could not create layer");
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

  public Collection LayerToSpeckle(Layer layer)
  {
    var collection = new Collection(layer.Name, "layer") { applicationId = layer.Id.ToString() };

    // add dynamic rhino props
    collection["visible"] = layer.IsVisible;

    return collection;
  }
}
