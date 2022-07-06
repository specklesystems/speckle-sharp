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
  public class PreviewConduit : Rhino.Display.DisplayConduit
  {
    public List<ApplicationObject> Preview { get; set; }

    protected override void PreDrawObjects(Rhino.Display.DrawEventArgs e)
    {
      // draw preview objects
      foreach (var previewObj in Preview)
      {
        if (previewObj.Convertible)
          Draw(previewObj, e.Display);
        else
          previewObj.Fallback.ForEach(o => Draw(o, e.Display));
      }
    }

    private void Draw(ApplicationObject obj, DisplayPipeline display)
    {
      var material = new DisplayMaterial();
      material.Transparency = 0.8;
      var vp = display.Viewport;
      bool wireMode = vp.DisplayMode.EnglishName.ToLower() == "wireframe" ? true : false;

      foreach (var convertedObj in obj.Converted) // these should be meshes and curves
      {
        switch (convertedObj)
        {
          case Brep o:
            if (wireMode)
              display.DrawBrepWires(o, material.Diffuse);
            else
              display.DrawBrepShaded(o, material);
            break;
          case Mesh o:
            if (wireMode)
              display.DrawMeshWires(o, material.Diffuse);
            else
              display.DrawMeshShaded(o, material);
            break;
          case Curve o:
            display.DrawCurve(o, material.Diffuse);
            break;
          case Rhino.Geometry.Point3d o:
            display.DrawPoint(o);
            break;
          case PointCloud o:
            display.DrawPointCloud(o, 1);
            break;
          case Hatch o:
            display.DrawHatch(o, material.Diffuse, material.Diffuse);
            break;
          case Text3d o:
            display.Draw3dText(o, material.Diffuse);
            break;
          case InstanceObject o:
            // todo: this needs to be handled, including how block defs are created during preview
            //obj.Rollback = true;
            break;
          case string o:
            // this means it was a view
            //obj.Rollback = true;
            break;
          default:
            break;
        }
      }
    }

  }

  #endregion

  public static class Formatting
  {
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
