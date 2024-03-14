using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Point = Rhino.Geometry.Point;

namespace SpeckleRhino;

public static class Utils
{
  public static string GetRhinoHostAppVersion() =>
    RhinoApp.Version.Major switch
    {
      6 => HostApplications.Rhino.GetVersion(HostAppVersion.v6),
      7 => HostApplications.Rhino.GetVersion(HostAppVersion.v7),
      8 => HostApplications.Rhino.GetVersion(HostAppVersion.v8),
      _ => throw new NotSupportedException($"Version {RhinoApp.Version.Major} of Rhino is not supported"),
    };

  public static string invalidRhinoChars = @"{}()[]";

  /// <summary>
  /// Creates a valid name for Rhino layers, blocks, and named views.
  /// </summary>
  /// <param name="str">Layer, block, or named view name</param>
  /// <returns>The original name if valid, or "@name" if not.</returns>
  /// <remarks>From trial and error, names cannot begin with invalidRhinoChars. This has been encountered in grasshopper branch syntax.</remarks>
  public static string MakeValidName(string str)
  {
    if (string.IsNullOrEmpty(str))
    {
      return str;
    }
    else
    {
      return invalidRhinoChars.Contains(str[0]) ? $"@{str}" : str;
    }
  }

  /// <summary>
  /// Creates a valid path for Rhino layers.
  /// </summary>
  /// <param name="str"></param>
  /// <returns></returns>
  public static string MakeValidPath(string str)
  {
    if (string.IsNullOrEmpty(str))
    {
      return str;
    }

    string validPath = "";
    string[] layerNames = str.Split(new string[] { Layer.PathSeparator }, StringSplitOptions.None);
    foreach (var item in layerNames)
    {
      validPath += string.IsNullOrEmpty(validPath) ? MakeValidName(item) : Layer.PathSeparator + MakeValidName(item);
    }

    return validPath;
  }

  /// <summary>
  /// Attemps to retrieve a Guid from a string
  /// </summary>
  /// <param name="s"></param>
  /// <returns>Guid on success, null on failure</returns>
  public static bool GetGuidFromString(string s, out Guid id)
  {
    id = Guid.Empty;
    if (string.IsNullOrEmpty(s))
    {
      return false;
    }

    try
    {
      id = Guid.Parse(s);
    }
    catch (FormatException)
    {
      return false;
    }

    return true;
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

    if (GetGuidFromString(id, out Guid guid))
    {
      if (doc.Objects.FindId(guid) is RhinoObject geom)
      {
        descriptor = Formatting.ObjectDescriptor(geom);
        obj = geom;
      }
      else
      {
        if (doc.Layers.FindId(guid) is Layer layer)
        {
          descriptor = "Layer";
          obj = layer;
        }
        else
        {
          if (doc.Views.Find(guid)?.ActiveViewport is RhinoViewport standardView)
          {
            descriptor = "Standard View";
            obj = new ViewInfo(standardView);
          }
        }
      }
    }
    else // this was probably a named view (saved by name, not guid)
    {
      var viewIndex = doc.NamedViews.FindByName(id);
      if (viewIndex != -1)
      {
        obj = doc.NamedViews[viewIndex];
        descriptor = "Named View";
      }
    }

    return obj != null;
  }

  #region extension methods

  /// <summary>
  /// Creates a layer from its name and parent
  /// </summary>
  /// <param name="doc"></param>
  /// <param name="path"></param>
  /// <returns>The new layer</returns>
  /// <exception cref="ArgumentException">Layer name is invalid.</exception>
  /// <exception cref="InvalidOperationException">Layer parent could not be set, or a layer with the same name already exists.</exception>
  public static Layer MakeLayer(this RhinoDoc doc, string name, Layer parentLayer = null)
  {
    if (!Layer.IsValidName(name))
    {
      throw new ArgumentException("Layer name is invalid.");
    }

    Layer newLayer = new() { Color = Color.AliceBlue, Name = name };
    if (parentLayer != null)
    {
      try
      {
        newLayer.ParentLayerId = parentLayer.Id;
      }
      catch (Exception e) when (!e.IsFatal())
      {
        throw new InvalidOperationException("Could not set layer parent id.", e);
      }
    }

    int newIndex = doc.Layers.Add(newLayer);
    if (newIndex is -1)
    {
      throw new InvalidOperationException("A layer with the same name already exists.");
    }

    return newLayer;
  }

  /// <summary>
  /// Finds a layer from its full path
  /// </summary>
  /// <param name="doc"></param>
  /// <param name="path">Full path of layer</param>
  /// <param name="makeIfNull">Create the layer if it doesn't already exist</param>
  /// <returns>The layer on success. On failure, returns null.</returns>
  /// <remarks>Note: The created layer path may be different from the input path, due to removal of invalid chars</remarks>
  public static Layer GetLayer(this RhinoDoc doc, string path, bool makeIfNull = false)
  {
    Layer layer;
    var cleanPath = MakeValidPath(path);
    int index = doc.Layers.FindByFullPath(cleanPath, RhinoMath.UnsetIntIndex);
    if (index is RhinoMath.UnsetIntIndex && makeIfNull)
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
        {
          try
          {
            currentLayer = doc.MakeLayer(layerNames[i], parent);
          }
          catch (ArgumentException argEx)
          {
            SpeckleLog.Logger.Error(
              argEx,
              "Failed to create layer {layerPath} with {exceptionMessage}",
              currentLayerPath,
              argEx.Message
            );
            RhinoApp.CommandLineOut.WriteLine(
              $"Failed to create layer {currentLayerPath} while creating {cleanPath}: {argEx.Message}"
            );
            break;
          }
          catch (InvalidOperationException ioEx)
          {
            SpeckleLog.Logger.Error(
              ioEx,
              "Failed to create layer {layerPath} with {exceptionMessage}",
              currentLayerPath,
              ioEx.Message
            );
            RhinoApp.CommandLineOut.WriteLine(
              $"Failed to create layer {currentLayerPath} while creating {cleanPath}: {ioEx.Message}"
            );
            break;
          }
        }

        parent = currentLayer;
      }

      layer = currentLayer;
    }
    else
    {
      layer = doc.Layers.FindIndex(index);
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
      List<object> toBeConverted = previewObj.Convertible
        ? previewObj.Converted
        : previewObj.Fallback?.SelectMany(o => o.Converted)?.ToList();

      if (toBeConverted is null)
      {
        continue;
      }

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

      if (!Preview.ContainsKey(previewObj.OriginalId) && converted.Count > 0)
      {
        Preview.Add(previewObj.OriginalId, converted);
      }
    }
  }

  public Dictionary<string, List<object>> Preview { get; set; } = new();

  public void SelectPreviewObject(string id, bool unselect = false)
  {
    if (Preview.ContainsKey(id))
    {
      if (unselect)
      {
        Selected.Remove(id);
      }
      else if (!Selected.Contains(id))
      {
        Selected.Add(id);
      }
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
    {
      return;
    }

    //e.Display.ZBiasMode = ZBiasMode.TowardsCamera;

    foreach (var id in ObjectIds)
    {
      if (id == null)
      {
        continue;
      }

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
    {
      return string.Empty;
    }

    var simpleType = obj.ObjectType.ToString();
    return obj.HasName ? $"{simpleType}" : $"{simpleType} {obj.Name}";
  }
}
