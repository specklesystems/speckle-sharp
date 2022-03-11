using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Speckle.Core.Kits;

using Rhino;
using Rhino.DocObjects;

namespace SpeckleRhino
{
  public static class Utils
  {
#if RHINO6
    public static string RhinoAppName = VersionedHostApplications.Rhino6;
    public static string AppName = "Rhino";
#elif RHINO7
    public static string RhinoAppName = VersionedHostApplications.Rhino7;
    public static string AppName = "Rhino";
#endif
    #region extension methods
    /// <summary>
    /// Finds a layer from its full path
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="path">Full path of layer</param>
    /// <param name="MakeIfNull">Create the layer if it doesn't already exist</param>
    /// <returns>Null on failure</returns>
    public static Layer GetLayer(this RhinoDoc doc, string path, bool MakeIfNull = false)
    {
      int index = doc.Layers.FindByFullPath(path, RhinoMath.UnsetIntIndex);
      Layer layer = doc.Layers.FindIndex(index);
      if (layer == null && MakeIfNull)
      {
        var layerNames = path.Split(new string[] { Layer.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);

        Layer parent = null;
        string currentLayerPath = string.Empty;
        Layer currentLayer = null;
        for (int i = 0; i < layerNames.Length; i++)
        {
          currentLayerPath = (i == 0) ? layerNames[i] : $"{currentLayerPath}{Layer.PathSeparator}{layerNames[i]}";
          currentLayer = doc.GetLayer(currentLayerPath);
          if (currentLayer == null)
            currentLayer = MakeLayer(doc, layerNames[i], parent);
          if (currentLayer == null)
            break;
          parent = currentLayer;
        }
        layer = currentLayer;
      }
      return layer;
    } 
    #endregion

    #region internal methods
    private static Layer MakeLayer(RhinoDoc doc, string name, Layer parentLayer = null)
    {
      Layer newLayer = new Layer() { Color = System.Drawing.Color.AliceBlue, Name = name };
      if (parentLayer != null)
        newLayer.ParentLayerId = parentLayer.Id;
      int newIndex = doc.Layers.Add(newLayer);
      if ( newIndex < 0)
        return null;
      else
        return doc.Layers.FindIndex(newIndex);
    }
    #endregion
  }
}
