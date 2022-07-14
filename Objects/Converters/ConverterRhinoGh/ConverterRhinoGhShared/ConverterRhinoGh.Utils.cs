using Grasshopper.Kernel.Types;
using Objects.Geometry;
using Objects.Primitive;
using Objects.Other;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
        args = parsedSchema[1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).ToArray();
      return parsedSchema[0].Trim();
    }

    private string GetCommitInfo()
    {
      var segments = Doc.Notes.Split(new string[] { "%%%" }, StringSplitOptions.None).ToList();
      return segments.Count > 1 ? segments[1] : "Unknown commit";
    }

    #region app props
    public static string RhinoPropName = "RhinoProps";

    private Base GetRhinoProps(GeometryBase o, Type t, bool getParentProps = false, List<string> excludedProps = null)
    {
      var appProps = new Base();
      appProps["class"] = t.Name;

      // set primitive writeable props 
      foreach (var propInfo in t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
      {
        if (excludedProps != null && excludedProps.Contains(propInfo.Name)) continue;
        if (IsMeaningfulProp(propInfo, o, out object propValue))
          appProps[propInfo.Name] = propValue;
      }
      if (getParentProps)
      {
        foreach (var propInfo in t.BaseType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
        {
          if (excludedProps != null && excludedProps.Contains(propInfo.Name)) continue;
          if (IsMeaningfulProp(propInfo, o, out object propValue))
            appProps[propInfo.Name] = propValue;
        }
      }

      return appProps;
    }
    private bool IsMeaningfulProp(PropertyInfo propInfo, GeometryBase o, out object value)
    {
      value = propInfo.GetValue(o);
      if (propInfo.GetSetMethod() != null && value != null)
      {
        if (propInfo.PropertyType.IsPrimitive || propInfo.PropertyType == typeof(decimal)) return true;
        if (propInfo.PropertyType == typeof(string) && !string.IsNullOrEmpty((string)value)) return true;
        if (propInfo.PropertyType.BaseType.Name == "Enum") // for some reason "IsEnum" prop returns false
        {
          value = value.ToString(); 
          return true;
        } 
      }
      return false;
    }

    // Scaled props need to be scaled to native units
    private void SetRhinoProps(object o, Type t, Base props, List<string>scaledProps = null, string units = null)
    {
      var propNames = props.GetDynamicMembers();
      if (o == null || propNames.Count() == 0)
        return;

      var typeProperties = t.GetProperties().ToList();
      typeProperties.AddRange(t.BaseType.GetProperties().ToList());
      foreach (var propInfo in typeProperties)
      {
        if (propInfo.CanWrite && propNames.Contains(propInfo.Name))
        {
          var value = props[propInfo.Name];
          if (scaledProps != null && scaledProps.Contains(propInfo.Name)) 
            value = ScaleToNative((double)value, units);
          if (propInfo.PropertyType.BaseType.Name == "Enum")
            value = Enum.Parse(propInfo.PropertyType, (string)value);
          if (value != null)
            try
            {
              t.InvokeMember(propInfo.Name,
              BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
              Type.DefaultBinder, o, new object[] { value });
            }
            catch { }
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
