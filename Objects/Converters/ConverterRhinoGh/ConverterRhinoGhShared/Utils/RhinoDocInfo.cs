using System;
using System.Drawing;
using System.Linq;
using Rhino;
using Rhino.DocObjects;
using Rhino.UI;
using Speckle.Core.Models;

namespace Objects.Converter.RhinoGh.Utils;

// better name would be useful, could be split out, feels a bit big :D
public sealed class RhinoDocInfo : IRhinoDocInfo
{
  // why public?
  public const string INVALID_RHINO_CHARS = @"{}()";

  /// <summary>
  /// Removes invalid characters for Rhino layer and block names
  /// </summary>
  /// <param name="str"></param>
  /// <returns></returns>
  public string RemoveInvalidRhinoChars(string str)
  {
    // using this to handle grasshopper branch syntax
    return str.Replace("{", "").Replace("}", "");
  }

  public string GetCommitInfo(RhinoDoc doc)
  {
    var segments = doc.Notes.Split(new[] { "%%%" }, StringSplitOptions.None).ToList();
    return segments.Count > 1 ? segments[1] : "Unknown commit"; // localisation?
  }

  public int GetMaterialIndex(RhinoDoc doc, string name)
  {
    if (string.IsNullOrEmpty(name))
      return -1;

    // if MaterialIndex also works...
    //   var material = doc.Materials.FirstOrDefault(x => x.Name == name);
    //   return material != null ? material.MaterialIndex : -1;

    for (int i = 0; i < doc.Materials.Count; i++)
    {
      if (doc.Materials[i].Name == name)
      {
        return i;
      }
    }

    return -1;
  }

  public Layer GetLayer(RhinoDoc doc, string path, out int index, bool MakeIfNull = false)
  {
    index = doc.Layers.FindByFullPath(path, RhinoMath.UnsetIntIndex);
    Layer layer = doc.Layers.FindIndex(index);

    if (layer == null && MakeIfNull)
    {
      var layerNames = path.Split(new[] { Layer.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);

      Layer parent = null;
      string currentLayerPath = string.Empty;
      Layer currentLayer = null;
      for (int i = 0; i < layerNames.Length; i++)
      {
        currentLayerPath = i == 0 ? layerNames[i] : $"{currentLayerPath}{Layer.PathSeparator}{layerNames[i]}";
        currentLayer = GetLayer(doc, currentLayerPath, out index);

        if (currentLayer == null)
        {
          currentLayer = MakeLayer(doc, layerNames[i], out index, parent);
        }

        if (currentLayer == null)
        {
          break;
        }

        parent = currentLayer;
      }

      layer = currentLayer;
    }
    return layer;
  }

  public Layer MakeLayer(RhinoDoc doc, string name, out int index, Layer parentLayer = null)
  {
    index = -1;
    Layer newLayer = new() { Color = Color.White, Name = name };

    if (parentLayer != null)
    {
      newLayer.ParentLayerId = parentLayer.Id;
    }

    int newIndex = doc.Layers.Add(newLayer);

    if (newIndex < 0)
    {
      return null;
    }

    return doc.Layers.FindIndex(newIndex);
  }
}
