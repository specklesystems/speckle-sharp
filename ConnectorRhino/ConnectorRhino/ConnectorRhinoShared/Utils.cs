using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Point = Rhino.Geometry.Point;

namespace SpeckleRhino;

public static class Utils
{
#if RHINO6
  public static string RhinoAppName = HostApplications.Rhino.GetVersion(HostAppVersion.v6);
  public static string AppName = "Rhino";
#elif RHINO7
    public static string RhinoAppName = HostApplications.Rhino.GetVersion(HostAppVersion.v7);
    public static string AppName = "Rhino";
#else
  public static string RhinoAppName = HostApplications.Rhino.Name;
  public static string AppName = "Rhino";
#endif

  public static string invalidRhinoChars = @"{}()";

  /// <summary>
  /// Removes invalid characters for Rhino layer and block names
  /// </summary>
  /// <param name="str"></param>
  /// <returns></returns>
  public static string RemoveInvalidRhinoChars(string str)
  {
    // using this to handle grasshopper branch syntax
    string cleanStr = str.Replace("{", "").Replace("}", "");
    return cleanStr;
  }

  /// <summary>
  /// Tries to retrieve a doc object from its selected id. THis can be a RhinoObject, Layer, or ViewInfo
  /// </summary>
  /// <param name="doc"></param>
  /// <param name="id"></param>
  /// <param name="obj"></param>
  /// <param name="descriptor">The descriptor of this object, used for reporting</param>
  /// <returns>True if successful, false if not</returns>
  public static bool FindObjectBySelectedId(RhinoDoc doc, string id, out object obj, out string descriptor)
  {
    descriptor = string.Empty;
    obj = null;
    try
    {
      Guid guid = new(id); // try to get guid from object id

      RhinoObject geom = doc.Objects.FindId(guid);
      if (geom != null)
      {
        descriptor = Formatting.ObjectDescriptor(geom);
        obj = geom;
      }
      else
      {
        var layer = doc.Layers.FindId(guid);
        if (layer != null)
        {
          descriptor = "Layer";
          obj = layer;
        }
        else
        {
          var standardView = doc.Views.Find(guid)?.ActiveViewport;
          if (standardView != null)
          {
            descriptor = "Standard View";
            obj = new ViewInfo(standardView);
          }
        }
      }
    }
    catch // this was a named view name
    {
      var viewIndex = doc.NamedViews.FindByName(id);
      if (viewIndex != -1)
      {
        obj = doc.NamedViews[viewIndex];
        descriptor = "Named View";
      }
    }

    return obj == null ? false : true;
  }

  #region extension methods
  /// <summary>
  /// Finds a layer from its full path
  /// </summary>
  /// <param name="doc"></param>
  /// <param name="path">Full path of layer</param>
  /// <param name="MakeIfNull">Create the layer if it doesn't already exist</param>
  /// <returns>Null on failure</returns>
  /// <remarks>Note: The created layer path may be different from the input path, due to removal of invalid chars</remarks>
  public static Layer GetLayer(this RhinoDoc doc, string path, bool MakeIfNull = false)
  {
    Layer MakeLayer(string name, Layer parentLayer = null)
    {
      try
      {
        Layer newLayer = new() { Color = Color.AliceBlue, Name = name };
        if (parentLayer != null)
          newLayer.ParentLayerId = parentLayer.Id;
        int newIndex = doc.Layers.Add(newLayer);
        if (newIndex < 0)
          return null;
        return doc.Layers.FindIndex(newIndex);
      }
      catch (Exception e)
      {
        return null;
      }
    }

    var cleanPath = RemoveInvalidRhinoChars(path);
    int index = doc.Layers.FindByFullPath(cleanPath, RhinoMath.UnsetIntIndex);
    Layer layer = doc.Layers.FindIndex(index);
    if (layer == null && MakeIfNull)
    {
      var layerNames = cleanPath.Split(new[] { Layer.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);

      Layer parent = null;
      string currentLayerPath = string.Empty;
      Layer currentLayer = null;
      for (int i = 0; i < layerNames.Length; i++)
      {
        currentLayerPath = i == 0 ? layerNames[i] : $"{currentLayerPath}{Layer.PathSeparator}{layerNames[i]}";
        currentLayer = doc.GetLayer(currentLayerPath);
        if (currentLayer == null)
          currentLayer = MakeLayer(layerNames[i], parent);
        if (currentLayer == null)
          break;
        parent = currentLayer;
      }
      layer = currentLayer;
    }
    return layer;
  }

  /// <summary>
  /// Get the NamedViews in the specified doc.
  /// </summary>
  /// <param name="doc">A RhinoDoc.</param>
  /// <returns>A string List of NamedViews name.</returns>
  public static List<string> NamedViews(this RhinoDoc doc)
  {
    List<string> views = doc.NamedViews.Select(v => v.Name).ToList();

    return views;
  }

  /// <summary>
  /// Get the StandardViews in the specified doc.
  /// </summary>
  /// <param name="doc">A RhinoDoc.</param>
  /// <returns>A string List of ViewportID.</returns>
  public static List<string> StandardViews(this RhinoDoc doc)
  {
    List<string> views = doc.Views.GetStandardRhinoViews().Select(v => v.ActiveViewportID.ToString()).ToList();

    return views;
  }
  #endregion
}

#region Preview
public class PreviewConduit : DisplayConduit
{
  public BoundingBox bbox;
  private Color color = Color.FromArgb(200, 59, 130, 246);
  private DisplayMaterial material;
  private List<string> Selected = new();
  private Color selectedColor = Color.FromArgb(200, 255, 255, 0);

  public PreviewConduit(List<ApplicationObject> preview)
  {
    material = new DisplayMaterial();
    material.Transparency = 0.8;
    material.Diffuse = color;
    bbox = new BoundingBox();

    foreach (var previewObj in preview)
    {
      var converted = new List<object>();
      var toBeConverted = previewObj.Convertible
        ? previewObj.Converted
        : previewObj.Fallback.SelectMany(o => o.Converted).ToList();
      foreach (var obj in toBeConverted)
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
        }
        converted.Add(obj);
      }

      if (!Preview.ContainsKey(previewObj.OriginalId))
        Preview.Add(previewObj.OriginalId, converted);
    }
  }

  private Dictionary<string, List<object>> Preview { get; set; } = new();

  public void SelectPreviewObject(string id, bool unselect = false)
  {
    if (Preview.ContainsKey(id))
    {
      if (unselect)
        Selected.Remove(id);
      else if (!Selected.Contains(id))
        Selected.Add(id);
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
    CalculateBoundingBox(e);
  }

  protected override void PreDrawObjects(DrawEventArgs e)
  {
    // draw preview objects
    var display = e.Display;

    foreach (var previewobj in Preview)
    {
      var drawColor = Selected.Contains(previewobj.Key) ? selectedColor : color;
      var drawMaterial = material;
      drawMaterial.Diffuse = drawColor;
      foreach (var obj in previewobj.Value)
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
          case Point o:
            display.DrawPoint(o.Location, drawColor);
            break;
          case Point3d o:
            display.DrawPoint(o, drawColor);
            break;
          case PointCloud o:
            display.DrawPointCloud(o, 5, drawColor);
            break;
        }
    }
  }
}

#endregion

public class MappingsDisplayConduit : DisplayConduit
{
  public List<string> ObjectIds { get; set; } = new();

  public Color Color { get; set; } = Color.RoyalBlue;

  protected override void DrawOverlay(DrawEventArgs e)
  {
    base.DrawOverlay(e);
    if (!Enabled)
      return;

    //e.Display.ZBiasMode = ZBiasMode.TowardsCamera;

    foreach (var id in ObjectIds)
    {
      if (id == null)
        continue;
      var obj = RhinoDoc.ActiveDoc.Objects.FindId(new Guid(id));
      switch (obj.ObjectType)
      {
        case ObjectType.Curve:
          e.Display.DrawCurve((Curve)obj.Geometry, Color);
          break;
        case ObjectType.Mesh:
          DisplayMaterial mMaterial = new(Color, 0.5);
          e.Display.DrawMeshShaded(obj.Geometry as Mesh, mMaterial);
          break;
        case ObjectType.Extrusion:
          DisplayMaterial eMaterial = new(Color, 0.5);
          e.Display.DrawBrepShaded(((Extrusion)obj.Geometry).ToBrep(), eMaterial);
          break;
        case ObjectType.Brep:
          DisplayMaterial bMaterial = new(Color, 0.5);
          e.Display.DrawBrepShaded((Brep)obj.Geometry, bMaterial);
          break;
      }
    }
  }
}

public static class Formatting
{
  public static string ObjectDescriptor(RhinoObject obj)
  {
    if (obj == null)
      return string.Empty;
    var simpleType = obj.ObjectType.ToString();
    return obj.HasName ? $"{simpleType}" : $"{simpleType} {obj.Name}";
  }

  public static string TimeAgo(string timestamp)
  {
    //TODO: this implementation is almost the same as Speckle.Core.Api.Helpers
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
