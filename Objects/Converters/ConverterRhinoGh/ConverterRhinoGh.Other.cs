using Grasshopper.Kernel.Types;
using Objects.Geometry;
using Objects.Primitive;
using Rhino.Geometry;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry.Collections;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using System;
using System.Collections.Generic;
using System.Linq;
using BlockDefinition = Objects.Other.BlockDefinition;
using BlockInstance = Objects.Other.BlockInstance;
using RH = Rhino.DocObjects;
using Rhino;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh
  {
    public BlockDefinition BlockDefinitionToSpeckle(RH.InstanceDefinition definition)
    {
      var geometry = new List<Base>();
      foreach (var obj in definition.GetObjects())
      {
        Base converted = ConvertToSpeckle(obj);
        if (converted != null)
        {
          converted["Layer"] = Doc.Layers[obj.Attributes.LayerIndex].FullPath;
          geometry.Add(converted);
        }
      }

      var _definition = new BlockDefinition()
      {
        name = definition.Name,
        basePoint = PointToSpeckle(Point3d.Origin),
        geometry = geometry,
        units = ModelUnits
      };

      return _definition;
    }

    public InstanceDefinition BlockDefinitionToNative(BlockDefinition definition)
    {
      // see if block name already exists and return if so
      if (Doc.InstanceDefinitions.Find(definition.name) is InstanceDefinition def)
        return def;

      // base point
      Point3d basePoint = PointToNative(definition.basePoint).Location;

      // geometry and attributes
      var geometry = new List<GeometryBase>();
      var attributes = new List<ObjectAttributes>();
      foreach (var geo in definition.geometry)
      {
        var converted = ConvertToNative(geo) as GeometryBase;
        if (converted == null)
          continue;
        var layerName = $"{Doc.Notes}{Layer.PathSeparator}{geo["Layer"] as string}";
        int index = 1;
        if (layerName != null)
          GetLayer(Doc, layerName, out index, true);
        var attribute = new ObjectAttributes()
        {
          LayerIndex = index
        };
        geometry.Add(converted);
        attributes.Add(attribute);
      }

      int definitionIndex = Doc.InstanceDefinitions.Add(definition.name, string.Empty, basePoint, geometry, attributes);

      if (definitionIndex < 0)
        return null;

      return Doc.InstanceDefinitions[definitionIndex];
    }

    public BlockInstance BlockInstanceToSpeckle(RH.InstanceObject instance)
    {
      var t = instance.InstanceXform;
      var transformArray = new double[] { 
        t.M00, t.M01, t.M02, t.M03, 
        t.M10, t.M11, t.M12, t.M13, 
        t.M20, t.M21, t.M22, t.M23, 
        t.M30, t.M31, t.M32, t.M33 };

      var def = BlockDefinitionToSpeckle(instance.InstanceDefinition);

      var _instance = new BlockInstance()
      {
        insertionPoint = PointToSpeckle(instance.InsertionPoint),
        transform = transformArray,
        blockDefinition = def,
        units = ModelUnits
      };

      return _instance;
    }

    public InstanceObject BlockInstanceToNative(BlockInstance instance)
    {
      // get the block definition
      InstanceDefinition definition = BlockDefinitionToNative(instance.blockDefinition);

      // get the transform
      if (instance.transform.Length != 16)
        return null;
      Transform transform = new Transform();
      transform.M00 = instance.transform[0];
      transform.M01 = instance.transform[1];
      transform.M02 = instance.transform[2];
      transform.M03 = instance.transform[3];
      transform.M10 = instance.transform[4];
      transform.M11 = instance.transform[5];
      transform.M12 = instance.transform[6];
      transform.M13 = instance.transform[7];
      transform.M20 = instance.transform[8];
      transform.M21 = instance.transform[9];
      transform.M22 = instance.transform[10];
      transform.M23 = instance.transform[11];
      transform.M30 = instance.transform[12];
      transform.M31 = instance.transform[13];
      transform.M32 = instance.transform[14];
      transform.M33 = instance.transform[15];

      // create the instance
      if (definition == null)
        return null;
      Guid instanceId = Doc.Objects.AddInstanceObject(definition.Index, transform);

      if (instanceId == Guid.Empty)
        return null;

      return Doc.Objects.FindId(instanceId) as InstanceObject;
    }

    private static Layer GetLayer(RhinoDoc doc, string path, out int index, bool MakeIfNull = false)
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
      Layer newLayer = new Layer() { Color = System.Drawing.Color.AliceBlue, Name = name };
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
  }
}