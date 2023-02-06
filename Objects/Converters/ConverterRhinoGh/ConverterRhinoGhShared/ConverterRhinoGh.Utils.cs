using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

using Rhino;
using Rhino.DocObjects;
using Grasshopper.Kernel.Types;

using Speckle.Core.Kits;
using Speckle.Core.Models;
using Objects.Geometry;
using Objects.Primitive;
using Objects.Other;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh
  {
    private string GetSchema(RhinoObject obj, out string[] args)
    {
      args = null;

      // user string has format "DirectShape{[family], [type]}" if it is a directshape conversion
      // user string has format "AdaptiveComponent{[family], [type]}" if it is an adaptive component conversion
      // user string has format "Pipe{[diameter]}" if it is a pipe conversion
      // user string has format "Duct{[width], [height], [diameter]}" if it is a duct conversion
      // otherwise, it is just the schema type name
      string schema = obj.Attributes.GetUserString(SpeckleSchemaKey);

      if (schema == null)
        return null;

      string[] parsedSchema = schema.Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
      if (parsedSchema.Length > 2) // there is incorrect formatting in the schema string!
        return null;
      if (parsedSchema.Length == 2)
        args = parsedSchema[1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)?.Select(o => o.Trim())?.ToArray();
      return parsedSchema[0].Trim();
    }

    private string GetCommitInfo()
    {
      var segments = Doc.Notes.Split(new string[] { "%%%" }, StringSplitOptions.None).ToList();
      return segments.Count > 1 ? segments[1] : "Unknown commit";
    }

    #region app props
    public static string RhinoPropName = "RhinoProps";
    private static string UserStrings = "userStrings";
    private static string UserDictionary = "userDictionary";

    /// <summary>
    /// Attaches user strings, user dictionaries, and object name from a RhinoObject to  Base
    /// </summary>
    /// <param name="obj">The converted Base object to attach info to</param>
    /// <param name="nativeObj">The Rhino object containing info</param>
    /// <returns></returns>
    public void GetUserInfo(Base obj, ObjectAttributes attributes, out List<string> notes)
    {
      notes = new List<string>();

      // user strings
      var userStrings = attributes.GetUserStrings();
      if (userStrings.Count > 0)
      {
        var userStringsBase = new Base();
        foreach (var key in userStrings.AllKeys)
        {
          try
          {
            userStringsBase[key] = userStrings[key];
          }
          catch (Exception e)
          {
            notes.Add($"Could not attach user string: {e.Message}");
          }
        }
        obj[UserStrings] = userStringsBase;
      }

      // user dictionary
      var userDictionaryBase = new Base();
      ParseArchivableToDictionary(userDictionaryBase, attributes.UserDictionary);
      obj[UserDictionary] = userDictionaryBase;

      // obj name
      if (!string.IsNullOrEmpty(attributes.Name)) obj["name"] = attributes.Name;
    }

    /// <summary>
    /// Copies an ArchivableDictionary to a Base
    /// </summary>
    /// <param name="target"></param>
    /// <param name="dict"></param>
    private void ParseArchivableToDictionary(Base target, Rhino.Collections.ArchivableDictionary dict)
    {
      foreach (var key in dict.Keys)
      {
        var obj = dict[key];
        switch (obj)
        {
          case Rhino.Collections.ArchivableDictionary o:
            var nested = new Base();
            ParseArchivableToDictionary(nested, o);
            target[key] = nested;
            continue;

          case double _:
          case bool _:
          case int _:
          case string _:
          case IEnumerable<double> _:
          case IEnumerable<bool> _:
          case IEnumerable<int> _:
          case IEnumerable<string> _:
            target[key] = obj;
            continue;

          default:
            continue;
        }
      }
    }
    #endregion

    #region Units

    /// <summary>
    /// Computes the Speckle Units of the current Document. The Rhino document is passed as a reference, so it will always be up to date.
    /// </summary>    
    public string ModelUnits => UnitToSpeckle(Doc.ModelUnitSystem);

    private void SetUnits(Base geom)
    {
      geom["units"] = ModelUnits;
    }

    private double ScaleToNative(double value, string units)
    {
      var f = Units.GetConversionFactor(units, ModelUnits);
      return value * f;
    }

    private string UnitToSpeckle(UnitSystem us)
    {
      switch (us)
      {
        case UnitSystem.None:
          return Units.Meters;
        //case UnitSystem.Angstroms:
        //  break;
        //case UnitSystem.Nanometers:
        //  break;
        //case UnitSystem.Microns:
        //  break;
        case UnitSystem.Millimeters:
          return Units.Millimeters;
        case UnitSystem.Centimeters:
          return Units.Centimeters;
        //case UnitSystem.Decimeters:
        //  break;
        case UnitSystem.Meters:
          return Units.Meters;
        //case UnitSystem.Dekameters:
        //  break;
        //case UnitSystem.Hectometers:
        //  break;
        case UnitSystem.Kilometers:
          return Units.Kilometers;
        //case UnitSystem.Megameters:
        //  break;
        //case UnitSystem.Gigameters:
        //  break;
        //case UnitSystem.Microinches:
        //  break;
        //case UnitSystem.Mils:
        //  break;
        case UnitSystem.Inches:
          return Units.Inches;
        case UnitSystem.Feet:
          return Units.Feet;
        case UnitSystem.Yards:
          return Units.Yards;
        case UnitSystem.Miles:
          return Units.Miles;
        //case UnitSystem.PrinterPoints:
        //  break;
        //case UnitSystem.PrinterPicas:
        //  break;
        //case UnitSystem.NauticalMiles:
        //  break;
        //case UnitSystem.AstronomicalUnits:
        //  break;
        //case UnitSystem.LightYears:
        //  break;
        //case UnitSystem.Parsecs:
        //  break;
        //case UnitSystem.CustomUnits:
        //  break;
        case UnitSystem.Unset:
          return Units.Meters;
        default:
          throw new Speckle.Core.Logging.SpeckleException($"The Unit System \"{us}\" is unsupported.");
      }
    }
    #endregion

    #region Layers
    public static Layer GetLayer(RhinoDoc doc, string path, out int index, bool MakeIfNull = false)
    {
      index = doc.Layers.FindByFullPath(path, RhinoMath.UnsetIntIndex);
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
          currentLayer = GetLayer(doc, currentLayerPath, out index);
          if (currentLayer == null)
            currentLayer = MakeLayer(doc, layerNames[i], out index, parent);
          if (currentLayer == null)
            break;
          parent = currentLayer;
        }
        layer = currentLayer;
      }
      return layer;
    }

    private static Layer MakeLayer(RhinoDoc doc, string name, out int index, Layer parentLayer = null)
    {
      index = -1;
      Layer newLayer = new Layer() { Color = System.Drawing.Color.White, Name = name };
      if (parentLayer != null)
        newLayer.ParentLayerId = parentLayer.Id;
      int newIndex = doc.Layers.Add(newLayer);
      if (newIndex < 0)
        return null;
      else
      {
        index = newIndex;
        return doc.Layers.FindIndex(newIndex);
      }
    }
    #endregion
  }
}
