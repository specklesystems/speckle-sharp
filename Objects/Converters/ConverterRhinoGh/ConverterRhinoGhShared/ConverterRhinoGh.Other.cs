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
using System.Drawing;
using System.Linq;
using BlockDefinition = Objects.Other.BlockDefinition;
using BlockInstance = Objects.Other.BlockInstance;
using DisplayStyle = Objects.Other.DisplayStyle;
using Hatch = Objects.Other.Hatch;
using HatchLoop = Objects.Other.HatchLoop;
using Polyline = Objects.Geometry.Polyline;
using Text = Objects.Other.Text;
using RH = Rhino.DocObjects;
using RenderMaterial = Objects.Other.RenderMaterial;
using Rhino;
using Rhino.Render;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh
  {
    public ObjectAttributes DisplayStyleToNative(DisplayStyle display)
    {
      var attributes = new ObjectAttributes();

      attributes.ColorSource = ObjectColorSource.ColorFromObject;
      attributes.ObjectColor = System.Drawing.Color.FromArgb(display.color);
      attributes.PlotWeight = display.lineweight;
      attributes.LinetypeSource = ObjectLinetypeSource.LinetypeFromObject;
      var lineStyle = Doc.Linetypes.FindName(display.linetype);
      attributes.LinetypeIndex = (lineStyle != null) ? lineStyle.Index : 0;

      return attributes;
    }

    public DisplayStyle DisplayStyleToSpeckle(ObjectAttributes attributes)
    {
      var style = new DisplayStyle();

      style.color = attributes.DrawColor(Doc).ToArgb();
      var lineType = Doc.Linetypes[attributes.LinetypeIndex];
      if (lineType.HasName)
        style.linetype = lineType.Name;
      style.lineweight = attributes.PlotWeight;

      return style;
    }

    public Rhino.Render.RenderMaterial RenderMaterialToNative(RenderMaterial speckleMaterial)
    {
      var commitInfo = GetCommitInfo();
      var speckleName = $"{commitInfo} - {speckleMaterial.name}";
      
      // check if the doc already has a material with speckle material name, or a previously created speckle material
      var existing = Doc.RenderMaterials.FirstOrDefault(x => x.Name == speckleMaterial.name);
      if (existing != null)
        return existing;
      else
        existing = Doc.RenderMaterials.FirstOrDefault(x => x.Name == speckleName);
      if (existing != null)
        return existing;

      Rhino.Render.RenderMaterial rm;
//#if RHINO6
      var rhinoMaterial = new Material
      {
        Name = speckleName,
        DiffuseColor = Color.FromArgb(speckleMaterial.diffuse),
        EmissionColor = Color.FromArgb(speckleMaterial.emissive),
        Transparency = 1 - speckleMaterial.opacity
      };
      rm = Rhino.Render.RenderMaterial.CreateBasicMaterial(rhinoMaterial, Doc);
//#else
      //TODO Convert materials as PhysicallyBasedMaterial 
      // var pbrRenderMaterial = RenderContentType.NewContentFromTypeId(ContentUuids.PhysicallyBasedMaterialType, Doc) as Rhino.Render.RenderMaterial;
      // RH.Material simulatedMaterial = pbrRenderMaterial.SimulatedMaterial(RenderTexture.TextureGeneration.Allow);
      // RH.PhysicallyBasedMaterial pbr = simulatedMaterial.PhysicallyBased;
      //
      // pbr.BaseColor = ARBGToColor4f(speckleMaterial.diffuse);
      // pbr.Emission = ARBGToColor4f(speckleMaterial.emissive);
      // pbr.Opacity = speckleMaterial.opacity;
      // pbr.Metallic = speckleMaterial.metalness;
      // pbr.Roughness = speckleMaterial.roughness;
      //
      // rm = Rhino.Render.RenderMaterial.FromMaterial(pbr.Material, Doc);
      // rm.Name = speckleName;
//#endif
      
      Doc.RenderMaterials.Add(rm);

      return rm;
    }
    public RenderMaterial RenderMaterialToSpeckle(Material material)
    {
      var renderMaterial = new RenderMaterial();
      if (material == null) return renderMaterial;

      renderMaterial.name = material.Name ?? "default"; // default rhino material has no name or id
#if RHINO6
      
      renderMaterial.diffuse = material.DiffuseColor.ToArgb();
      renderMaterial.emissive = material.EmissionColor.ToArgb();
      renderMaterial.opacity = 1 - material.Transparency;
      
      // for some reason some default material transparency props are 1 when they shouldn't be - use this hack for now
      if ((renderMaterial.name.ToLower().Contains("glass") || renderMaterial.name.ToLower().Contains("gem")) && renderMaterial.opacity == 0)
        renderMaterial.opacity = 0.3;
#else
      Material matToUse = material;
      if(!material.IsPhysicallyBased)
      {
        matToUse = new Material();
        matToUse.CopyFrom(material);
        matToUse.ToPhysicallyBased();
      }
      using (var rm = Rhino.Render.RenderMaterial.FromMaterial(matToUse, null))
      {
        RH.PhysicallyBasedMaterial pbrMaterial = rm.ConvertToPhysicallyBased(RenderTexture.TextureGeneration.Allow);
        renderMaterial.diffuse = pbrMaterial.BaseColor.AsSystemColor().ToArgb();
        renderMaterial.emissive = pbrMaterial.Emission.AsSystemColor().ToArgb();
        renderMaterial.opacity = pbrMaterial.Opacity;
        renderMaterial.metalness = pbrMaterial.Metallic;
        renderMaterial.roughness = pbrMaterial.Roughness;
      }
#endif
      
      return renderMaterial;
    }

    public Rhino.Geometry.Hatch[] HatchToNative(Hatch hatch)
    {

      var curves = new List<Rhino.Geometry.Curve>();
      curves = (hatch.loops != null) ? hatch.loops.Select(o => CurveToNative(o.Curve)).ToList() : hatch.curves.Select(o => CurveToNative(o)).ToList();
      var pattern = Doc.HatchPatterns.FindName(hatch.pattern);
      int index;
      if (pattern == null)
      {
        // find default hatch pattern
        pattern = FindDefaultPattern(hatch.pattern);
        index = Doc.HatchPatterns.Add(pattern);
      }
      else
        index = pattern.Index;
      var hatches = Rhino.Geometry.Hatch.Create(curves, index, hatch.rotation, hatch.scale, 0.001);

      return hatches;
    }
    public Hatch HatchToSpeckle(Rhino.Geometry.Hatch hatch)
    {
      var _hatch = new Hatch();

      // retrieve hatch loops
      var loops = new List<HatchLoop>();
      foreach (var outer in hatch.Get3dCurves(true).ToList())
        loops.Add(new HatchLoop(CurveToSpeckle(outer), Other.HatchLoopType.Outer));
      foreach (var inner in hatch.Get3dCurves(false).ToList())
        loops.Add(new HatchLoop(CurveToSpeckle(inner), Other.HatchLoopType.Inner));

      _hatch.loops = loops;
      _hatch.scale = hatch.PatternScale;
      _hatch.pattern = Doc.HatchPatterns.ElementAt(hatch.PatternIndex).Name;
      _hatch.rotation = hatch.PatternRotation;

      return _hatch;
    }
    private HatchPattern FindDefaultPattern(string patternName)
    {
      var defaultPattern = typeof(HatchPattern.Defaults).GetProperties()?.Where(o => o.Name.Equals(patternName, StringComparison.OrdinalIgnoreCase))?.ToList().FirstOrDefault();
      if (defaultPattern != null)
        return defaultPattern.GetValue(this, null) as HatchPattern;
      else
        return HatchPattern.Defaults.Solid;
    }

    public BlockDefinition BlockDefinitionToSpeckle(RH.InstanceDefinition definition)
    {
      var geometry = new List<Base>();
      foreach (var obj in definition.GetObjects())
      {
        if (CanConvertToSpeckle(obj))
        {
          Base converted = ConvertToSpeckle(obj);
          if (converted != null)
          {
            converted["Layer"] = Doc.Layers[obj.Attributes.LayerIndex].FullPath;
            geometry.Add(converted);
          }
        }
      }

      var _definition = new BlockDefinition()
      {
        name = definition.Name,
        basePoint = PointToSpeckle(Point3d.Origin), // rhino by default sets selected block def base pt at world origin
        geometry = geometry,
        units = ModelUnits
      };

      return _definition;
    }

    public InstanceDefinition BlockDefinitionToNative(BlockDefinition definition)
    {
      // get modified definition name with commit info
      var commitInfo = GetCommitInfo();
      var blockName = $"{commitInfo} - {definition.name}";

      // see if block name already exists and return if so
      if (Doc.InstanceDefinitions.Find(blockName) is InstanceDefinition def)
        return def;

      // base point
      Point3d basePoint = PointToNative(definition.basePoint).Location;

      // geometry and attributes
      var geometry = new List<GeometryBase>();
      var attributes = new List<ObjectAttributes>();
      foreach (var geo in definition.geometry)
      {
        if (CanConvertToNative(geo))
        {
          List<GeometryBase> converted = new List<GeometryBase>();
          switch (geo)
          {
            case BlockInstance o:
              var instance = BlockInstanceToNative(o);
              if (instance != null)
              {
                converted.Add(instance.DuplicateGeometry());
                Doc.Objects.Delete(instance);
              }
              break;
            default:
              var convertedObj = ConvertToNative(geo);
              if (convertedObj.GetType().IsArray)
                foreach (object o in (Array)convertedObj)
                  converted.Add((GeometryBase)o);
              else
                converted.Add((GeometryBase)convertedObj);
              break;
          }
          if (converted.Count == 0)
            continue;
          var layerName = (geo["Layer"] != null) ? $"{commitInfo}{Layer.PathSeparator}{geo["Layer"] as string}" : $"{commitInfo}";
          int index = 1;
          if (layerName != null)
            GetLayer(Doc, layerName, out index, true);

          var attribute = new ObjectAttributes();
          if (geo[@"displayStyle"] is Base display)
          {
            if (ConvertToNative(display) is ObjectAttributes displayAttribute)
              attribute = displayAttribute;
          }
          else if (geo[@"renderMaterial"] is Base renderMaterial)
          {
            if (renderMaterial["diffuse"] is int color)
            {
              attribute.ColorSource = ObjectColorSource.ColorFromObject;
              attribute.ObjectColor = Color.FromArgb(color);
            }
          }
          attribute.LayerIndex = index;

          geometry.AddRange(converted);
          attributes.Add(attribute);
        }
      }

      int definitionIndex = Doc.InstanceDefinitions.Add(blockName, string.Empty, basePoint, geometry, attributes);

      if (definitionIndex < 0)
        return null;

      var blockDefinition = Doc.InstanceDefinitions[definitionIndex];

      return blockDefinition;
    }

    // Rhino convention seems to order the origin of the vector space last instead of first
    // This results in a transposed transformation matrix - may need to be addressed later
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
        transform = new Other.Transform(transformArray, ModelUnits),
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
      // rhino doesn't seem to handle transform matrices where the translation vector last value is a divisor instead of 1, so make sure last value is set to 1
      Transform transform = Transform.Identity;
      double[] t = instance.transform.value;
      if (t.Length == 16)
      {
        int count = 0;
        for (int i = 0; i < 4; i++)
        {
          for (int j = 0; j < 4; j++)
          {
            if (j == 3) // scale the delta values for translation transformations and set last value (divisor) to 1
              if (t[15] != 0)
                transform[i, j] = (i != 3) ? ScaleToNative(t[count] / t[15], instance.units) : 1;
              else
                transform[i, j] = (i != 3) ? ScaleToNative(t[count], instance.units) : 1;
            else
              transform[i, j] = t[count];
            count++;
          }
        }
      }

      // create the instance
      if (definition == null)
        return null;
      Guid instanceId = Doc.Objects.AddInstanceObject(definition.Index, transform);

      if (instanceId == Guid.Empty)
        return null;

      return Doc.Objects.FindId(instanceId) as InstanceObject;
    }

    // Text
    public Text TextToSpeckle(TextEntity text)
    {
      var _text = new Text();

      // display value as list of polylines
      var outlines = text.CreateCurves(text.DimensionStyle, false)?.ToList();
      if (outlines != null)
      {
        foreach (var outline in outlines)
        {
          Rhino.Geometry.Polyline poly = null;
          if (!outline.TryGetPolyline(out poly))
            outline.ToPolyline(0, 1, 0, 0, 0, 0.1, 0, 0, true).TryGetPolyline(out poly); // this is from nurbs, should probably be refined for text
          if (poly != null)
            _text.displayValue.Add(PolylineToSpeckle(poly) as Polyline);
        }
      }

      _text.plane = PlaneToSpeckle(text.Plane);
      _text.rotation = text.TextRotationRadians;
      _text.height = text.TextHeight * text.DimensionScale; // this needs to be multiplied by model space scale for true height
      _text.value = text.PlainText;
      _text.richText = text.RichText;
      _text.units = ModelUnits;

      // rhino specific props
      _text["horizontalAlignment"] = text.TextHorizontalAlignment.ToString();
      _text["verticalAlignment"] = text.TextVerticalAlignment.ToString();

      return _text;
    }
    public TextEntity TextToNative(Text text)
    {
      var _text = new TextEntity();
      _text.Plane = PlaneToNative(text.plane);
      if (!string.IsNullOrEmpty(text.richText))
        _text.RichText = text.richText;
      else
        _text.PlainText = text.value;
      _text.TextHeight = ScaleToNative(text.height, text.units);
      _text.TextRotationRadians = text.rotation;
      
      // rhino specific props
      if (text["horizontalAlignment"] != null)
        _text.TextHorizontalAlignment = Enum.TryParse(text["horizontalAlignment"] as string, out TextHorizontalAlignment horizontal) ? horizontal : TextHorizontalAlignment.Center;
      if (text["verticalAlignment"] != null)
        _text.TextVerticalAlignment = Enum.TryParse(text["verticalAlignment"] as string, out TextVerticalAlignment vertical) ? vertical : TextVerticalAlignment.Middle;

      return _text;
    }

    public Color4f ARBGToColor4f(int argb)
    {
      var systemColor = Color.FromArgb(argb);
      return Color4f.FromArgb(systemColor.A, systemColor.R, systemColor.G, systemColor.B);
    }
  }
}