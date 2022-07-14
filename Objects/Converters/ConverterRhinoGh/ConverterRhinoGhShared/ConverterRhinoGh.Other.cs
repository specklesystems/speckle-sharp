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
using Arc = Objects.Geometry.Arc;
using BlockDefinition = Objects.Other.BlockDefinition;
using BlockInstance = Objects.Other.BlockInstance;
using DisplayStyle = Objects.Other.DisplayStyle;
using Dimension = Objects.Other.Dimension;
using Hatch = Objects.Other.Hatch;
using HatchLoop = Objects.Other.HatchLoop;
using Line = Objects.Geometry.Line;
using Point = Objects.Geometry.Point;
using Polyline = Objects.Geometry.Polyline;
using Text = Objects.Other.Text;
using RH = Rhino.DocObjects;
using RenderMaterial = Objects.Other.RenderMaterial;
using Rhino;
using Rhino.Render;
using Objects.Other;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh
  {
    public ObjectAttributes DisplayStyleToNative(DisplayStyle display)
    {
      var attributes = new ObjectAttributes();

      attributes.ColorSource = ObjectColorSource.ColorFromObject;
      attributes.ObjectColor = System.Drawing.Color.FromArgb(display.color);
      attributes.PlotWeightSource = ObjectPlotWeightSource.PlotWeightFromObject;
      var conversionFactor = (display.units == null) ? 1 : Units.GetConversionFactor(Units.GetUnitsFromString(display.units), Units.Millimeters);
      attributes.PlotWeight = display.lineweight * conversionFactor;
      attributes.LinetypeSource = ObjectLinetypeSource.LinetypeFromObject;
      var lineStyle = Doc.Linetypes.FindName(display.linetype);
      attributes.LinetypeIndex = (lineStyle != null) ? lineStyle.Index : 0;

      return attributes;
    }

    public DisplayStyle DisplayStyleToSpeckle(ObjectAttributes attributes)
    {
      var style = new DisplayStyle();

      // color
      switch (attributes.ColorSource)
      {
        case ObjectColorSource.ColorFromObject:
          style.color = attributes.ObjectColor.ToArgb();
          break;
        case ObjectColorSource.ColorFromMaterial:
          style.color = Doc.Materials[attributes.MaterialIndex].DiffuseColor.ToArgb();
          break;
        default: // use layer color as default
          style.color = Doc.Layers[attributes.LayerIndex].Color.ToArgb();
          break;
      }

      // line type
      Linetype lineType = null;
      switch (attributes.LinetypeSource)
      {
        case ObjectLinetypeSource.LinetypeFromObject:
          lineType = Doc.Linetypes[attributes.LinetypeIndex];
          break;
        default: // use layer linetype as default
          lineType = Doc.Linetypes[Doc.Layers[attributes.LayerIndex].LinetypeIndex];
          break;
      }
      if (lineType.HasName)
        style.linetype = lineType.Name;

      // line weight
      switch (attributes.PlotWeightSource)
      {
        case ObjectPlotWeightSource.PlotWeightFromObject:
          style.lineweight = attributes.PlotWeight;
          break;
        default: // use layer lineweight as default
          style.lineweight = Doc.Layers[attributes.LayerIndex].PlotWeight;
          break;
      }
      if (style.lineweight == 0) style.lineweight = 0.25;

      style.units = Units.Millimeters;

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
      var rhinoMaterial = new RH.Material
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
    public RenderMaterial RenderMaterialToSpeckle(RH.Material material)
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
      RH.Material matToUse = material;
      if(!material.IsPhysicallyBased)
      {
        matToUse = new RH.Material();
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
      var iT = instance.transform;
      var units = instance.units;
      var transform = TransformToNative(iT, units);

      // create the instance
      if (definition == null)
        return null;
      Guid instanceId = Doc.Objects.AddInstanceObject(definition.Index, transform);

      if (instanceId == Guid.Empty)
        return null;

      return Doc.Objects.FindId(instanceId) as InstanceObject;
    }

    public DisplayMaterial RenderMaterialToDisplayMaterial(RenderMaterial material)
    {
      var rhinoMaterial = new Material
      {
        Name = material.name,
        DiffuseColor = Color.FromArgb(material.diffuse),
        EmissionColor = Color.FromArgb(material.emissive),
        Transparency = 1 - material.opacity
      };
      var displayMaterial = new DisplayMaterial(rhinoMaterial);
      return displayMaterial;
    }
    
    
    public RenderMaterial DisplayMaterialToSpeckle(DisplayMaterial material)
    {
      var speckleMaterial = new RenderMaterial();
      speckleMaterial.diffuse = material.Diffuse.ToArgb();
      speckleMaterial.emissive = material.Emission.ToArgb();
      speckleMaterial.opacity = 1.0 - material.Transparency;
      return speckleMaterial;
    }
    
    public Transform TransformToNative(Other.Transform speckleTransform, string units = null)
    {
      var u = units ?? speckleTransform.units;
      var transform = Transform.Identity;
      var t = speckleTransform.value;
      if (t.Length != 16) return transform;
      var count = 0;
      for (var i = 0; i < 4; i++)
      {
        for (var j = 0; j < 4; j++)
        {
          if (j == 3) // scale the delta values for translation transformations and set last value (divisor) to 1
            if (t[15] != 0)
              transform[i, j] = (i != 3) ? ScaleToNative(t[count] / t[15], u) : 1;
            else
              transform[i, j] = (i != 3) ? ScaleToNative(t[count], u) : 1;
          else
            transform[i, j] = t[count];
          count++;
        }
      }
      return transform;
    }

    public Other.Transform TransformToSpeckle(Transform t, string units = null)
    {
      var u = units ?? ModelUnits;
      var transformArray = new double[] {
        t.M00, t.M01, t.M02, t.M03,
        t.M10, t.M11, t.M12, t.M13,
        t.M20, t.M21, t.M22, t.M23,
        t.M30, t.M31, t.M32, t.M33 };
      return new Other.Transform(transformArray, ModelUnits);
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
      var excludeProps = new List<string>()
      {
        "Text",
        "TextRotationRadians",
        "PlainText",
        "RichText",
        "FontIndex"
      };
      var props = GetRhinoProps(text, typeof(TextEntity), true, excludeProps);
      var style = text.DimensionStyle.HasName ? text.DimensionStyle.Name : String.Empty;
      if (!string.IsNullOrEmpty(style)) props["DimensionStyleName"] = style;
      _text[RhinoPropName] = props;

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
      Base sourceAppProps = text[RhinoPropName] as Base;
      if (sourceAppProps != null)
      {
        var scaleProps = new List<string>() { "TextHeight" };
        SetRhinoProps(_text, typeof(TextEntity), sourceAppProps, scaleProps, text.units);
        DimensionStyle dimensionStyle = Doc.DimStyles.FindName(sourceAppProps["DimensionStyleName"] as string ?? string.Empty);
        if (dimensionStyle != null)
          _text.DimensionStyleId = dimensionStyle.Id;
      }
      return _text;
    }


    // Dimension
    public Dimension DimensionToSpeckle(Rhino.Geometry.Dimension dimension)
    {
      Dimension _dimension = null;
      Base props = null;

      switch (dimension)
      {
        case LinearDimension o:
          if (o.Get3dPoints(out Point3d extensionStart, out Point3d extensionEnd, out Point3d linearStartArrow, out Point3d linearEndArrow, out Point3d linearDimPoint, out Point3d linearTextPoint))
          {
            var linearDimension = new DistanceDimension() { units = ModelUnits, text = dimension.PlainText, measurement = dimension.NumericValue };
            linearDimension.direction = VectorToSpeckle(o.Plane.XAxis);
            linearDimension.position = PointToSpeckle(linearDimPoint);
            linearDimension.measured = new List<Point>() { PointToSpeckle(extensionStart), PointToSpeckle(extensionEnd) };
            if (o.GetDisplayLines(o.DimensionStyle, o.DimensionScale, out IEnumerable<Rhino.Geometry.Line> lines))
              linearDimension.displayValue = lines.Select(l => LineToSpeckle(l) as ICurve).ToList();

            props = GetRhinoProps(o, typeof(LinearDimension), true);
            props["TextPosition"] = PointToSpeckle(linearTextPoint);

            _dimension = linearDimension;
          }
          break;
        case AngularDimension o:
          if (o.Get3dPoints(out Point3d angularCenter, out Point3d defStart, out Point3d defEnd, out Point3d angularStartArrow, out Point3d angularEndArrow, out Point3d angularDimPoint, out Point3d angularTextPoint))
          {
            var angularDimension = new AngleDimension() { units = ModelUnits, text = dimension.PlainText, measurement = dimension.NumericValue };
            angularDimension.position = PointToSpeckle(angularDimPoint);
            angularDimension.measured = new List<Line>() { LineToSpeckle(new Rhino.Geometry.Line(angularCenter, defStart)), LineToSpeckle(new Rhino.Geometry.Line(angularCenter, defEnd))};
            if (o.GetDisplayLines(o.DimensionStyle, o.DimensionScale, out Rhino.Geometry.Line[] lines, out Rhino.Geometry.Arc[] arcs))
            {
              angularDimension.displayValue = lines.Select(l => LineToSpeckle(l) as ICurve).ToList();
              angularDimension.displayValue.AddRange(arcs.Select(a => ArcToSpeckle(a) as ICurve).ToList());
            }

            props = GetRhinoProps(o, typeof(AngularDimension), true);
            props["TextPosition"] = PointToSpeckle(angularTextPoint);

            _dimension = angularDimension;
          }
          break;
        case OrdinateDimension o:
          if (o.Get3dPoints(out Point3d basePoint, out Point3d ordinateDefPoint, out Point3d leader, out Point3d kink1Point, out Point3d kink2Point))
          {
            var ordinateDimension = new DistanceDimension() { units = ModelUnits, text = dimension.PlainText, measurement = dimension.NumericValue };
            ordinateDimension.direction = o.Plane.XAxis == Vector3d.XAxis ? VectorToSpeckle(Vector3d.XAxis) : VectorToSpeckle(Vector3d.YAxis);
            ordinateDimension.position = PointToSpeckle(leader);
            ordinateDimension.measured = new List<Point>() { PointToSpeckle(basePoint), PointToSpeckle(ordinateDefPoint) };
            if (o.GetDisplayLines(o.DimensionStyle, o.DimensionScale, out IEnumerable<Rhino.Geometry.Line> lines))
              ordinateDimension.displayValue = lines.Select(l => LineToSpeckle(l) as ICurve).ToList();

            props = GetRhinoProps(o, typeof(OrdinateDimension), true);
            props["TextPosition"] = PointToSpeckle(new Point3d(o.TextPosition.X, o.TextPosition.Y, o.Plane.OriginZ));
            _dimension = ordinateDimension;
          }
          break;
        case RadialDimension o:
          if (o.Get3dPoints(out Point3d radialCenter, out Point3d radius, out Point3d radialDimPoint, out Point3d kneePoint))
          {
            var radialDimension = new LengthDimension() { units = ModelUnits, text = dimension.PlainText, measurement = dimension.NumericValue };
            radialDimension.position = PointToSpeckle(radialDimPoint);
            radialDimension.measured = LineToSpeckle(new Rhino.Geometry.Line(radialCenter, radius));
            if (o.GetDisplayLines(o.DimensionStyle, o.DimensionScale, out IEnumerable<Rhino.Geometry.Line> lines))
              radialDimension.displayValue = lines.Select(l => LineToSpeckle(l) as ICurve).ToList();

            props = GetRhinoProps(o, typeof(RadialDimension), true);
            props["TextPosition"] = PointToSpeckle(new Point3d(o.TextPosition.X, o.TextPosition.Y, o.Plane.OriginZ));

            _dimension = radialDimension;
          }
          break;
      }

      if (_dimension != null && props != null)
      {
        _dimension.units = ModelUnits;
        var style = dimension.DimensionStyle.HasName ? dimension.DimensionStyle.Name : String.Empty;
        if (!string.IsNullOrEmpty(style)) props["DimensionStyleName"] = style;
        _dimension[RhinoPropName] = props;
      }
      return _dimension;
    }

    public Rhino.Geometry.Dimension RhinoDimensionToNative(Dimension dimension)
    {
      Rhino.Geometry.Dimension _dimension = null;
      Base sourceAppProps = dimension[RhinoPropName] as Base;
      if (sourceAppProps == null) return DimensionToNative(dimension);

      var position = PointToNative(dimension.position).Location;
      Point textPosition = sourceAppProps["TextPosition"] != null ? sourceAppProps["TextPosition"] as Point : dimension.position;
      string dimensionStyleName = sourceAppProps["DimensionStyleName"] as string != null ? sourceAppProps["DimensionStyleName"] as string : Doc.DimStyles.Current.Name;
      DimensionStyle dimensionStyle = Doc.DimStyles.FindName(dimensionStyleName);
      var plane = Rhino.Geometry.Plane.WorldYZ;
      plane.OriginZ = position.Z;

      string className = sourceAppProps != null ? sourceAppProps["class"] as string : string.Empty;
      switch (className)
      {
        case "LinearDimension":
          _dimension = DimensionToNative(dimension);
          SetRhinoProps(_dimension, typeof(LinearDimension), sourceAppProps);
          break;
        case "AngularDimension":
          _dimension = DimensionToNative(dimension);
          SetRhinoProps(_dimension, typeof(AngularDimension), sourceAppProps);
          break;
        case "OrdinateDimension":
          var ordinateSpeckle = dimension as DistanceDimension;
          if (ordinateSpeckle == null || ordinateSpeckle.measured.Count < 2 || ordinateSpeckle.direction == null) goto default;
          var ordinateBase = PointToNative(ordinateSpeckle.measured[0]).Location;
          var ordinateDefining = PointToNative(ordinateSpeckle.measured[1]).Location;
          var direction = VectorToNative(ordinateSpeckle.direction);
          var measuredDirection = direction == Vector3d.XAxis ? OrdinateDimension.MeasuredDirection.Xaxis : OrdinateDimension.MeasuredDirection.Yaxis;
          double kink1 = sourceAppProps["KinkOffset1"] as double? ?? 0;
          double kink2 = sourceAppProps["KinkOffset2"] as double? ?? 0;
          _dimension = OrdinateDimension.Create(dimensionStyle, plane, measuredDirection, ordinateBase, ordinateDefining, position, kink1, kink2);
          SetRhinoProps(_dimension, typeof(OrdinateDimension), sourceAppProps);
          break;
        case "RadialDimension":
          var radialSpeckle = dimension as LengthDimension;
          if (radialSpeckle == null || radialSpeckle.measured as Line == null) goto default;
          var radialLine = LineToNative(radialSpeckle.measured as Line);
          _dimension = RadialDimension.Create(dimensionStyle, AnnotationType.Radius, plane, radialLine.PointAtStart, radialLine.PointAtEnd, position);
          SetRhinoProps(_dimension, typeof(RadialDimension), sourceAppProps);
          break;
        default:
          _dimension = DimensionToNative(dimension);
          break;
      }

      _dimension.DimensionStyleId = dimensionStyle.Id;
      var convertedTextPosition = PointToNative(textPosition);
      _dimension.TextPosition = new Point2d(convertedTextPosition.Location.X, convertedTextPosition.Location.Y);
      return _dimension;
    }

    public Rhino.Geometry.Dimension DimensionToNative(Dimension dimension)
    {
      Rhino.Geometry.Dimension _dimension = null;
      var style = Doc.DimStyles.Current;
      var position = PointToNative(dimension.position).Location;
      var plane = Rhino.Geometry.Plane.WorldYZ;
      plane.OriginZ = position.Z;
      string textValue = dimension.text;
      switch (dimension)
      {
        case LengthDimension o:
          switch (o.measured)
          {
            case Line l: 
              var radialLine = LineToNative(l);
              var radialDimension = RadialDimension.Create(style, AnnotationType.Radius, plane, radialLine.PointAtStart, radialLine.PointAtEnd, position); ;
              _dimension = radialDimension;
              break;
            default: // all other curve length types will have to return a generic annotation
              break;
          }
          break;
        case AngleDimension o:
          if (o.measured.Count < 2) break;
          var lineStart = LineToNative(o.measured[0]);
          var lineEnd = LineToNative(o.measured[1]);
          var angle = Vector3d.VectorAngle(lineStart.Line.Direction, lineEnd.Line.Direction);
          var angleArc = new Rhino.Geometry.Arc(lineStart.PointAtStart, lineStart.GetLength(), angle);
          var offset = position.DistanceTo(angleArc.ClosestPoint(position));

          _dimension = new AngularDimension(angleArc, offset);
          break;
        case DistanceDimension o:
          if (o.measured.Count < 2) break;
          var start = PointToNative(o.measured[0]).Location;
          var end = PointToNative(o.measured[1]).Location;
          var normal = VectorToNative(o.direction);
          var dir = new Vector3d(end.X - start.X, end.Y - start.Y, end.Z - start.Z);

          var linearDimension = new LinearDimension(plane, new Point2d(start.X, start.Y), new Point2d(end.X, end.Y), new Point2d(position.X, position.Y));
          if (normal.IsPerpendicularTo(dir))
          {
            dir.PerpendicularTo(dir);
            linearDimension.Rotate(Vector3d.VectorAngle(dir, normal), Vector3d.ZAxis, position);
          }
          _dimension = linearDimension;  
          break;
        default:
          break;
      }
      return _dimension;
    }

    public Color4f ARBGToColor4f(int argb)
    {
      var systemColor = Color.FromArgb(argb);
      return Color4f.FromArgb(systemColor.A, systemColor.R, systemColor.G, systemColor.B);
    }
  }
}