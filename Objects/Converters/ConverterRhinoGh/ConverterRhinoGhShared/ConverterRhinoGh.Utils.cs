using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using Rhino;
using Rhino.Collections;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
#if GRASSHOPPER
#endif

namespace Objects.Converter.RhinoGh;

public partial class ConverterRhinoGh
{
  public static string invalidRhinoChars = @"{}()[]";

  /// <summary>
  /// Creates a valid name for Rhino layers, blocks, and named views.
  /// </summary>
  /// <param name="str">Layer, block, or named view name</param>
  /// <returns>The original name if valid, or "@name" if not.</returns>
  /// <remarks>From trial and error, names cannot begin with invalidRhinoChars. This has been encountered in grasshopper branch syntax.</remarks>
  public static string MakeValidName(string str)
  {
    return string.IsNullOrEmpty(str)
      ? str
      : invalidRhinoChars.Contains(str[0])
        ? $"@{str}"
        : str;
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
  /// Retrieves the index of a render material in the document rendermaterialtable by name
  /// </summary>
  /// <param name="name"></param>
  /// <returns>Index of the rendermaterial, or -1 if no matches are found</returns>
  public int GetMaterialIndex(string name)
  {
    var index = -1;
    if (string.IsNullOrEmpty(name))
    {
      return index;
    }

    for (int i = 0; i < Doc.Materials.Count; i++)
    {
      if (Doc.Materials[i].Name == name)
      {
        index = i;
        break;
      }
    }

    return index;
  }

  private string GetCommitInfo()
  {
    var segments = Doc.Notes.Split(new[] { "%%%" }, StringSplitOptions.None).ToList();
    return segments.Count > 1 ? segments[1] : "Unknown commit";
  }

  #region app props

  public static string RhinoPropName = "RhinoProps";
  private static string UserStrings = "userStrings";
  private static string UserDictionary = "userDictionary";

  private void SetUserInfo(Base obj, ObjectAttributes attributes)
  {
    // set user strings
    if (obj[UserStrings] is Base userStrings)
    {
      foreach (var member in userStrings.GetMembers(DynamicBaseMemberType.Dynamic))
      {
        attributes.SetUserString(member.Key, member.Value as string);
      }
    }

    // set application id
    if (!string.IsNullOrWhiteSpace(obj.applicationId))
    {
      attributes.SetUserString(ApplicationIdKey, obj.applicationId);
    }

    // set name or label
    var name = obj["name"] as string ?? obj["label"] as string; // gridlines have a "label" prop instead of name?
    if (name != null)
    {
      attributes.Name = name;
    }

    // set revit parameters as user strings
    if (obj["parameters"] is Base parameters)
    {
      foreach (var member in parameters.GetMembers(DynamicBaseMemberType.Dynamic))
      {
        if (member.Value is Objects.BuiltElements.Revit.Parameter parameter)
        {
          var convertedParameter = ParameterToNative(parameter);
          var paramName = $"{convertedParameter.Item1}({member.Key})";
          attributes.SetUserString(paramName, convertedParameter.Item2);
        }
      }
    }
  }

  /// <summary>
  /// Attaches the provided user strings, user dictionaries, and and name to Base
  /// </summary>
  /// <param name="obj">The converted Base object to attach info to</param>
  /// <returns></returns>
  public void GetUserInfo(
    Base obj,
    out List<string> notes,
    ArchivableDictionary userDictionary = null,
    NameValueCollection userStrings = null,
    string name = null
  )
  {
    notes = new List<string>();

    // user strings
    if (userStrings != null && userStrings.Count > 0)
    {
      var userStringsBase = new Base();
      foreach (var key in userStrings.AllKeys)
      {
        try
        {
          userStringsBase[key] = userStrings[key];
        }
        catch (Exception ex) when (!ex.IsFatal())
        {
          notes.Add($"Could not attach user string: {ex.Message}");
        }
      }

      obj[UserStrings] = userStringsBase;
    }

    // user dictionary
    if (userDictionary != null && userDictionary.Count > 0)
    {
      var userDictionaryBase = new Base();
      ParseArchivableToDictionary(userDictionaryBase, userDictionary);
      obj[UserDictionary] = userDictionaryBase;
    }

    // obj name
    if (!string.IsNullOrEmpty(name))
    {
      obj["name"] = name;
    }
  }

  /// <summary>
  /// Copies an ArchivableDictionary to a Base
  /// </summary>
  /// <param name="target"></param>
  /// <param name="dict"></param>
  private void ParseArchivableToDictionary(Base target, ArchivableDictionary dict)
  {
    foreach (var key in dict.Keys)
    {
      var obj = dict[key];
      switch (obj)
      {
        case ArchivableDictionary o:
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
        throw new SpeckleException($"The Unit System \"{us}\" is unsupported.");
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
          currentLayer = MakeLayer(doc, layerNames[i], parent);
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

  /// <summary>
  /// Creates a layer from its name and parent
  /// </summary>
  /// <param name="doc"></param>
  /// <param name="name"></param>
  /// <param name="parentLayer"></param>
  /// <returns>The new layer</returns>
  /// <exception cref="ArgumentException">Layer name is invalid.</exception>
  /// <exception cref="InvalidOperationException">Layer parent could not be set, or a layer with the same name already exists.</exception>
  public static Layer MakeLayer(RhinoDoc doc, string name, Layer parentLayer = null)
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
      catch (Exception e)
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

  #endregion
}
