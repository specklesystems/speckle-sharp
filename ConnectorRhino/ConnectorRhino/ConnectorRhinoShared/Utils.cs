using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;

using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Display;

using Speckle.Core.Kits;
using Speckle.Core.Models;

using DesktopUI2.ViewModels;
using System.Drawing;
using System.Linq;

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
#else
    public static string RhinoAppName = Applications.Rhino7;
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

  #region Preview
  public class PreviewConduit : DisplayConduit
  {
    private Dictionary<string, List<object>> Preview { get; set; } = new Dictionary<string, List<object>>();
    private List<string> Selected = new List<string>();
    public BoundingBox bbox;
    private Color color = Color.FromArgb(200, 59, 130, 246);
    private Color selectedColor = Color.FromArgb(200, 255, 255, 0);
    private DisplayMaterial material;

    public PreviewConduit(List<ApplicationObject> preview)
    {
      material = new DisplayMaterial();
      material.Transparency = 0.8;
      material.Diffuse = color;
      bbox = new BoundingBox();

      foreach (var previewObj in preview)
      {
        var converted = new List<object>();
        foreach (var obj in previewObj.Converted)
        {
          switch (obj)
          {
            case GeometryBase o:
              bbox.Union(o.GetBoundingBox(false));
              break;
            case Text3d o:
              bbox.Union(o.BoundingBox);
              break;
            case InstanceObject o:
              // todo: this needs to be handled, including how block defs are created during preview
              //obj.Rollback = true;
              break;
            default:
              break;
          }
          converted.Add(obj);
        }
        Preview.Add(previewObj.OriginalId, converted);
      }
    }

    public void SelectPreviewObject(string id, bool unselect = false)
    {
      if (Preview.ContainsKey(id))
      {
        if (unselect)
          Selected.Remove(id);
        else
          if (!Selected.Contains(id)) Selected.Add(id);
      }
    }

    // reference: https://developer.rhino3d.com/api/RhinoCommon/html/M_Rhino_Display_DisplayConduit_CalculateBoundingBox.htm
    protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
    {
      base.CalculateBoundingBox(e);
      e.IncludeBoundingBox(bbox);
    }

    protected override void CalculateBoundingBoxZoomExtents(CalculateBoundingBoxEventArgs e)
    {
      this.CalculateBoundingBox(e);
    }

    protected override void PreDrawObjects(Rhino.Display.DrawEventArgs e)
    {
      // draw preview objects
      var display = e.Display;

      foreach (var previewobj in Preview)
      {
        var drawColor = Selected.Contains(previewobj.Key) ?  selectedColor : color;
        var drawMaterial = material;
        drawMaterial.Diffuse = drawColor;
        foreach (var obj in previewobj.Value)
        {
          switch (obj)
          {
            case Brep o:
              display.DrawBrepShaded(o, drawMaterial);
              break;
            case Mesh o:
              display.DrawMeshShaded(o, drawMaterial);
              break;
            case Curve o:
              display.DrawCurve(o, drawColor);
              break;
            case Rhino.Geometry.Point o:
              display.DrawPoint(o.Location, drawColor);
              break;
            case Point3d o:
              display.DrawPoint(o,drawColor);
              break;
            case PointCloud o:
              display.DrawPointCloud(o, 5, drawColor);
              break;
            default:
              break;
          }
        }
      }
    }

  }

  #endregion

  public static class Formatting
  {
    public static string ObjectDescriptor(RhinoObject obj)
    {
      var simpleType = obj.ObjectType.ToString();
      return obj.HasName ? $"{simpleType}" : $"{simpleType} {obj.Name}";
    }

    public static string TimeAgo(string timestamp)
    {
      TimeSpan timeAgo;
      try
      {
        timeAgo = DateTime.Now.Subtract(DateTime.Parse(timestamp));
      }
      catch (FormatException e)
      {
        Debug.WriteLine("Could not parse the string to a DateTime");
        return "";
      }

      if (timeAgo.TotalSeconds < 60)
        return "less than a minute ago";
      if (timeAgo.TotalMinutes < 60)
        return $"about {timeAgo.Minutes} minute{PluralS(timeAgo.Minutes)} ago";
      if (timeAgo.TotalHours < 24)
        return $"about {timeAgo.Hours} hour{PluralS(timeAgo.Hours)} ago";
      if (timeAgo.TotalDays < 7)
        return $"about {timeAgo.Days} day{PluralS(timeAgo.Days)} ago";
      if (timeAgo.TotalDays < 30)
        return $"about {timeAgo.Days / 7} week{PluralS(timeAgo.Days / 7)} ago";
      if (timeAgo.TotalDays < 365)
        return $"about {timeAgo.Days / 30} month{PluralS(timeAgo.Days / 30)} ago";

      return $"over {timeAgo.Days / 356} year{PluralS(timeAgo.Days / 356)} ago";
    }

    public static string PluralS(int num)
    {
      return num != 1 ? "s" : "";
    }

    public static string CommitInfo(string stream, string branch, string commitId)
    {
      string formatted = $"{stream}[ {branch} @ {commitId} ]";
      string clean = Regex.Replace(formatted, @"[^\u0000-\u007F]+", string.Empty).Trim(); // remove emojis and trim :( 
      return clean;
    }
  }
}
