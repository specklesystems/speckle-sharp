using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Render;
using RH = Rhino.DocObjects;

using Speckle.Core.Kits;
using Speckle.Core.Models;
using Utilities = Speckle.Core.Models.Utilities;
using Speckle.Core.Models.GraphTraversal;

using Objects.Other;
using Arc = Objects.Geometry.Arc;
using BlockDefinition = Objects.Other.BlockDefinition;
using BlockInstance = Objects.Other.BlockInstance;
using Dimension = Objects.Other.Dimension;
using DisplayStyle = Objects.Other.DisplayStyle;
using Hatch = Objects.Other.Hatch;
using HatchLoop = Objects.Other.HatchLoop;
using Line = Objects.Geometry.Line;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Polyline = Objects.Geometry.Polyline;
using RenderMaterial = Objects.Other.RenderMaterial;
using Text = Objects.Other.Text;
using Transform = Objects.Other.Transform;
using Objects.BuiltElements.Revit;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh
  {
    // display and render
    public ObjectAttributes DisplayStyleToNative(DisplayStyle display)
    {
      var attributes = new ObjectAttributes();

      // color
      attributes.ObjectColor = System.Drawing.Color.FromArgb(display.color);
      var colorSource = ObjectColorSource.ColorFromObject;
      if (display["colorSource"] != null) { Enum.TryParse(display["colorSource"] as string, out colorSource); }
      attributes.ColorSource = colorSource;

      // line type
      var lineStyle = Doc.Linetypes.FindName(display.linetype);
      attributes.LinetypeIndex = (lineStyle != null) ? lineStyle.Index : 0;
      var lineSource = ObjectLinetypeSource.LinetypeFromObject;
      if (display["lineSource"] != null) { Enum.TryParse(display["lineSource"] as string, out lineSource); }
      attributes.LinetypeSource = lineSource;

      // plot weight
      var conversionFactor = (display.units == null) ? 1 : Units.GetConversionFactor(Units.GetUnitsFromString(display.units), Units.Millimeters);
      attributes.PlotWeight = display.lineweight * conversionFactor;
      var weightSource = ObjectPlotWeightSource.PlotWeightFromObject;
      if (display["weightSource"] != null) { Enum.TryParse(display["weightSource"] as string, out weightSource); }
      attributes.PlotWeightSource = weightSource;

      return attributes;
    }

    public DisplayStyle DisplayStyleToSpeckle(ObjectAttributes attributes, Layer layer = null)
    {
      var style = new DisplayStyle() { units = Units.Millimeters};
      int color = Color.LightGray.ToArgb();
      Linetype lineType = null;
      double lineWeight = 0;
      string colorSource = null;
      string lineTypeSource = null;
      string weightSource = null;

      // use layer attributes if a layer is provided
      if (layer != null)
      {
        color = layer.Color.ToArgb();
        lineType = Doc.Linetypes[layer.LinetypeIndex];
        lineWeight = layer.PlotWeight;
      }
      else
      {
        // color
        colorSource = attributes.ColorSource.ToString();
        switch (attributes.ColorSource)
        {
          case ObjectColorSource.ColorFromObject:
            color = attributes.ObjectColor.ToArgb();
            break;
          case ObjectColorSource.ColorFromMaterial:
            color = Doc.Materials[attributes.MaterialIndex].DiffuseColor.ToArgb();
            break;
          default: // use layer color as default
            color = Doc.Layers[attributes.LayerIndex].Color.ToArgb();
            break;
        }

        // line type
        lineTypeSource = attributes.LinetypeSource.ToString();
        switch (attributes.LinetypeSource)
        {
          case ObjectLinetypeSource.LinetypeFromObject:
            lineType = Doc.Linetypes[attributes.LinetypeIndex];
            break;
          default: // use layer linetype as default
            lineType = Doc.Linetypes[Doc.Layers[attributes.LayerIndex].LinetypeIndex];
            break;
        }

        // line weight
        weightSource = attributes.PlotWeightSource.ToString();
        switch (attributes.PlotWeightSource)
        {
          case ObjectPlotWeightSource.PlotWeightFromObject:
            lineWeight = attributes.PlotWeight;
            break;
          default: // use layer lineweight as default
            lineWeight = Doc.Layers[attributes.LayerIndex].PlotWeight;
            break;
        }
      }

      style.color = color;
      style.lineweight = lineWeight;
      style.linetype = lineType?.Name ?? "Default";

      // attach rhino specific props
      if (colorSource != null) style["colorSource"] = colorSource;
      if (lineTypeSource != null) style["lineSource"] = lineTypeSource;
      if (weightSource != null) style["weightSource"] = weightSource;

      return style;
    }

    public Rhino.Render.RenderMaterial RenderMaterialToNative(RenderMaterial speckleMaterial)
    {
      var commitInfo = GetCommitInfo();
      var speckleName = ReceiveMode == ReceiveMode.Create ? $"{commitInfo} - {speckleMaterial.name}" : $"{speckleMaterial.name}";

      // check if the doc already has a material with speckle material name, or a previously created speckle material
      //NOTE: Looking up renderMaterials this way is slow, maybe we can create a dictionary?
      var existing = Doc.RenderMaterials.FirstOrDefault(x => x.Name == speckleName);
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
      Doc.Materials.Add(rhinoMaterial);
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

    // hatch
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
        loops.Add(new HatchLoop(CurveToSpeckle(outer), HatchLoopType.Outer));
      foreach (var inner in hatch.Get3dCurves(false).ToList())
        loops.Add(new HatchLoop(CurveToSpeckle(inner), HatchLoopType.Inner));

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

    // blocks
    public Rhino.Geometry.Transform TransformToNative(Transform transform)
    {
      var matrix = transform.ConvertToUnits(ModelUnits);
      var _transform = Rhino.Geometry.Transform.Identity;
      double homogeneousDivisor = matrix[15]; // rhino doesn't seem to handle transform matrices where the translation vector last value is a divisor instead of 1, so make sure last value is set to 1
      int count = 0;
      for (var i = 0; i < 4; i++)
      {
        for (var j = 0; j < 4; j++)
        {
          _transform[i, j] = (j == 3 && homogeneousDivisor != 1) ? matrix[count] / homogeneousDivisor : matrix[count];
          count++;
        }
      }

      return _transform;
    }

    public BlockDefinition BlockDefinitionToSpeckle(InstanceDefinition definition)
    {
      // check if this has been converted and cached already
      if (BlockDefinitions.ContainsKey(definition.Name))
        return BlockDefinitions[definition.Name];

      var geometry = new List<Base>();
      foreach (var obj in definition.GetObjects())
      {
        if (CanConvertToSpeckle(obj))
        {
          Base converted = ConvertToSpeckle(obj);
          if (converted != null)
          {
            converted["layer"] = Doc.Layers[obj.Attributes.LayerIndex].FullPath;
            geometry.Add(converted);
          }
        }
      }

      // rhino by default sets selected block def base pt at world origin
      var _definition = new BlockDefinition(definition.Name, geometry, PointToSpeckle(Point3d.Origin)) { units = ModelUnits, applicationId = definition.Id.ToString() };
      BlockDefinitions.Add(definition.Name, _definition);

      return _definition;
    }

    public InstanceDefinition DefinitionToNative(Base definition, out List<string> notes)
    {
      notes = new List<string>();

      // get the definition name
      var commitInfo = GetCommitInfo();
      string definitionName = 
        definition is BlockDefinition blockDef ? blockDef.name : 
        definition is RevitSymbolElementType revitDef ? $"{revitDef.family} - {revitDef.type} - {definition.id}" : 
        definition.id;
      if (ReceiveMode == ReceiveMode.Create) definitionName = $"{commitInfo} - " + definitionName;
      if (Doc.InstanceDefinitions.Find(definitionName) is InstanceDefinition def)
        return def;

      // get definition geometry to traverse and base point
      Point3d basePoint = Point3d.Origin;
      var toTraverse = new List<Base>();
      switch (definition)
      {
        case BlockDefinition o:
          if (o.basePoint != null)
            basePoint = PointToNative(o.basePoint).Location;
          toTraverse = o.geometry ?? (o["@geometry"] as List<object>).Cast<Base>().ToList();
          break;
        default:
          toTraverse.Add(definition);
          break;
      }

      // traverse definition geo to get convertible geo
      var conversionDict = new Dictionary<Base, string>();
      foreach (var obj in toTraverse)
      {
        var convertible = FlattenDefinitionObject(obj);
        foreach (var key in convertible.Keys)
        {
          if (!conversionDict.ContainsKey(key))
          {
            conversionDict.Add(key, convertible[key]);
          }
        }
      }

      // convert definition geometry and attributes
      var converted = new List<GeometryBase>();
      var attributes = new List<ObjectAttributes>();
      foreach (var item in conversionDict)
      {
        var geo = item.Key;
        var convertedGeo = new List<GeometryBase>();
        switch (geo)
        {
          case Instance o:
            var instanceNotes = new List<string>();
            var instanceAppObj = InstanceToNative(o, false);
            var instance = instanceAppObj.Converted.FirstOrDefault() as InstanceObject;
            if (instance != null)
            {
              converted.Add(instance.DuplicateGeometry());
              attributes.Add(instance.Attributes);
              Doc.Objects.Delete(instance);
            }
            else
            {
              notes.AddRange(instanceNotes);
              notes.Add($"Could not create nested Instance of definition {definitionName}");
            }
            break;
          default:
            var convertedObj = ConvertToNative(geo);
            if (convertedObj == null)
            {
              notes.Add($"Could not create definition geometry {geo.speckle_type} ({geo.id})");
              continue;
            }

            if (convertedObj.GetType().IsArray)
              foreach (object o in (Array)convertedObj)
                convertedGeo.Add((GeometryBase)o);
            else
              convertedGeo.Add((GeometryBase)convertedObj);
            break;
        }
        if (convertedGeo.Count == 0)
          continue;

        // get attributes
        var attribute = new ObjectAttributes();

        // layer
        var geoLayer = item.Key["layer"] is string s ?  s : item.Value; // blocks sent from rhino will have a layer prop dynamically attached
        var layerName = ReceiveMode == ReceiveMode.Create ? $"{commitInfo}{Layer.PathSeparator}{geoLayer}" : $"{geoLayer}";
        int index = 1;
        if (layerName != null)
          GetLayer(Doc, layerName, out index, true);
        attribute.LayerIndex = index;

        // display
        var renderMaterial = geo[@"renderMaterial"] as RenderMaterial;
        if (geo[@"displayStyle"] is DisplayStyle display)
        {
          attribute = DisplayStyleToNative(display);
        }
        else if (renderMaterial != null)
        {
          attribute.ObjectColor = Color.FromArgb(renderMaterial.diffuse);
          attribute.ColorSource = ObjectColorSource.ColorFromObject;
        }

        // render material
        if (renderMaterial != null)
        {
          var material = RenderMaterialToNative(renderMaterial);
          attribute.MaterialIndex = GetMaterialIndex(material?.Name);
          attribute.MaterialSource = ObjectMaterialSource.MaterialFromObject;
        }

        converted.AddRange(convertedGeo);
        for(int i = 0; i < convertedGeo.Count; i++)
          attributes.Add(attribute);
      }

      if (converted.Count == 0)
      {
        notes.Add("Could not convert any definition geometry");
        return null;
      }
      
      // add definition to the doc
      int definitionIndex = Doc.InstanceDefinitions.Add(definitionName, string.Empty, basePoint, converted, attributes);
      if (definitionIndex < 0)
      {
        notes.Add("Could not add definition to the document");
        return null;
      }
      var blockDefinition = Doc.InstanceDefinitions[definitionIndex];

      return blockDefinition;
    }

#region block def flattening
    /// <summary>
    /// Traverses the object graph, returning objects that can be converted.
    /// </summary>
    /// <param name="obj">The root <see cref="Base"/> object to traverse</param>
    /// <returns>A flattened list of objects to be converted ToNative</returns>
    private Dictionary<Base, string> FlattenDefinitionObject(Base obj)
    {
      var StoredObjects = new Dictionary<Base, string>();

      void StoreObject(Base current, string containerId)
      {
        //Handle convertable objects
        if (CanConvertToNative(current))
        {
          StoredObjects.Add(current, containerId);
          return;
        }

        //Handle objects convertable using displayValues
        var fallbackMember = current["displayValue"] ?? current["@displayValue"];
        if (fallbackMember != null)
        {
          GraphTraversal.TraverseMember(fallbackMember).ToList()
            .ForEach(o => StoreObject(o, containerId));
          return;
        }
      }

      string LayerId(TraversalContext context) => LayerIdRecurse(context, new StringBuilder()).ToString();
      StringBuilder LayerIdRecurse(TraversalContext context, StringBuilder stringBuilder)
      {
        if (context.propName == null) return stringBuilder;

        // see if there's a layer property on this obj
        var layer = context.current["layer"] as string ?? context.current["Layer"] as string;
        if (!string.IsNullOrEmpty(layer)) return new StringBuilder(layer);

        var objectLayerName = context.propName[0] == '@'
          ? context.propName.Substring(1)
          : context.propName;

        LayerIdRecurse(context.parent, stringBuilder);
        stringBuilder.Append(Layer.PathSeparator);
        stringBuilder.Append(objectLayerName);

        return stringBuilder;
      }

      var traverseFunction = DefaultTraversal.CreateTraverseFunc(this);

      traverseFunction.Traverse(obj).ToList()
        .ForEach(tc => StoreObject(tc.current, LayerId(tc)));

      return StoredObjects;
    }
#endregion

    // Rhino convention seems to order the origin of the vector space last instead of first
    // This results in a transposed transformation matrix - may need to be addressed later
    public BlockInstance BlockInstanceToSpeckle(InstanceObject instance)
    {
      var t = instance.InstanceXform.ToFloatArray(true);

      var def = BlockDefinitionToSpeckle(instance.InstanceDefinition);

      var _instance = new BlockInstance()
      {
        transform = new Transform(t, ModelUnits),
        typedDefinition = def,
        units = ModelUnits
      };

      return _instance;
    }

    public ApplicationObject InstanceToNative(Instance instance, bool AppendToModelSpace = true)
    {
      var appObj = new ApplicationObject(instance.id, instance.speckle_type) { applicationId = instance.applicationId };

      // get the definition
      var definition = instance.definition ?? instance["@definition"] as Base ?? instance["@blockDefinition"] as Base; // some applications need to dynamically attach defs (eg sketchup)
      if (definition == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "instance did not have a definition");
        return appObj;
      }

      // convert the definition
      InstanceDefinition instanceDef = DefinitionToNative(definition, out List<string> notes);
      if (notes.Count > 0) appObj.Update(log: notes);
      if (instanceDef == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Could not create block definition");
        return appObj;
      }

      // get the transform
      var transform = TransformToNative(instance.transform);

      // create the instance
      Guid instanceId = Doc.Objects.AddInstanceObject(instanceDef.Index, transform);

      if (instanceId == Guid.Empty)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Could not add instance to doc");
        return appObj;
      }

      var _instance = Doc.Objects.FindId(instanceId) as InstanceObject;

      // add application id
      try
      {
        _instance.Attributes.SetUserString(ApplicationIdKey, instance.applicationId);
      }
      catch (Exception e)
      {
        appObj.Update(logItem: $"Could not set application id user string: {e.Message}");
      }

      // update appobj
      appObj.Update(convertedItem: _instance);
      if (AppendToModelSpace)
        appObj.CreatedIds.Add(instanceId.ToString());
      return appObj;
    }

    public DisplayMaterial RenderMaterialToDisplayMaterial(RenderMaterial material)
    {
      var rhinoMaterial = new RH.Material
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

      // rhino props
      var ignore = new List<string>() {
        "Text",
        "TextRotationRadians",
        "PlainText",
        "RichText",
        "FontIndex" };
      var props = Utilities.GetApplicationProps(text, typeof(TextEntity), true, ignore);
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

      // rhino props
      Base sourceAppProps = text[RhinoPropName] as Base;
      if (sourceAppProps != null)
      {
        var scaleProps = new List<string>() {
          "TextHeight" };
        foreach (var scaleProp in scaleProps)
        {
          var value = sourceAppProps[scaleProp] as double?;
          if (value.HasValue)
            sourceAppProps[scaleProp] = ScaleToNative(value.Value, text.units);
        }
        Utilities.SetApplicationProps(_text, typeof(TextEntity), sourceAppProps);
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
      var ignore = new List<string>() {
        "Text",
        "PlainText",
        "RichText" };
      Point3d textPoint = new Point3d();

      switch (dimension)
      {
        case LinearDimension o:
          if (o.Get3dPoints(out Point3d linearStart, out Point3d linearEnd, out Point3d linearStartArrow, out Point3d linearEndArrow, out Point3d linearDimPoint, out textPoint))
          {
            var linearDimension = new DistanceDimension() { units = ModelUnits, measurement = dimension.NumericValue, isOrdinate = false };

            var normal = new Vector3d(linearEndArrow.X - linearStartArrow.X, linearEndArrow.Y - linearStartArrow.Y, linearEndArrow.Z - linearStartArrow.Z);
            normal.Rotate(Math.PI / 2, Vector3d.ZAxis);
            linearDimension.direction = VectorToSpeckle(normal);
            linearDimension.position = PointToSpeckle(linearDimPoint);
            linearDimension.measured = new List<Point>() { PointToSpeckle(linearStart), PointToSpeckle(linearEnd) };
            if (o.GetDisplayLines(o.DimensionStyle, o.DimensionScale, out IEnumerable<Rhino.Geometry.Line> lines))
              linearDimension.displayValue = lines.Select(l => LineToSpeckle(l) as ICurve).ToList();

            props = Utilities.GetApplicationProps(o, typeof(LinearDimension), true, ignore);
            _dimension = linearDimension;
          }
          break;
        case AngularDimension o:
          if (o.Get3dPoints(out Point3d angularCenter, out Point3d angularStart, out Point3d angularEnd, out Point3d angularStartArrow, out Point3d angularEndArrow, out Point3d angularDimPoint, out textPoint))
          {
            var lineStart = LineToSpeckle(new Rhino.Geometry.Line(angularCenter, angularStart));
            var lineEnd = LineToSpeckle(new Rhino.Geometry.Line(angularCenter, angularEnd));

            var angularDimension = new AngleDimension() { units = ModelUnits, measurement = (Math.PI / 180) * dimension.NumericValue };
            angularDimension.position = PointToSpeckle(angularDimPoint);
            angularDimension.measured = new List<Line>() { lineStart, lineEnd };
            if (o.GetDisplayLines(o.DimensionStyle, o.DimensionScale, out Rhino.Geometry.Line[] lines, out Rhino.Geometry.Arc[] arcs))
            {
              angularDimension.displayValue = lines.Select(l => LineToSpeckle(l) as ICurve).ToList();
              angularDimension.displayValue.AddRange(arcs.Select(a => ArcToSpeckle(a) as ICurve).ToList());
            }

            props = Utilities.GetApplicationProps(o, typeof(AngularDimension), true, ignore);
            _dimension = angularDimension;
          }
          break;
        case OrdinateDimension o:
          if (o.Get3dPoints(out Point3d basePoint, out Point3d ordinateDefPoint, out Point3d leader, out Point3d kink1Point, out Point3d kink2Point))
          {
            var ordinateDimension = new DistanceDimension() { units = ModelUnits, measurement = dimension.NumericValue, isOrdinate = true };
            ordinateDimension.direction = Math.Round(Math.Abs(ordinateDefPoint.X - basePoint.X) - o.NumericValue) == 0 ? VectorToSpeckle(Vector3d.XAxis) : VectorToSpeckle(Vector3d.YAxis);
            ordinateDimension.position = PointToSpeckle(leader);
            ordinateDimension.measured = new List<Point>() { PointToSpeckle(basePoint), PointToSpeckle(ordinateDefPoint) };
            if (o.GetDisplayLines(o.DimensionStyle, o.DimensionScale, out IEnumerable<Rhino.Geometry.Line> lines))
              ordinateDimension.displayValue = lines.Select(l => LineToSpeckle(l) as ICurve).ToList();
            textPoint = new Point3d(o.Plane.OriginX + o.TextPosition.X, o.Plane.OriginZ + o.TextPosition.Y, o.Plane.OriginZ);
            props = Utilities.GetApplicationProps(o, typeof(OrdinateDimension), true, ignore);
            _dimension = ordinateDimension;
          }
          break;
        case RadialDimension o:
          if (o.Get3dPoints(out Point3d radialCenter, out Point3d radius, out Point3d radialDimPoint, out Point3d kneePoint))
          {
            var radialDimension = new LengthDimension() { units = ModelUnits, measurement = dimension.NumericValue };
            radialDimension.position = PointToSpeckle(radialDimPoint);
            radialDimension.measured = LineToSpeckle(new Rhino.Geometry.Line(radialCenter, radius));
            if (o.GetDisplayLines(o.DimensionStyle, o.DimensionScale, out IEnumerable<Rhino.Geometry.Line> lines))
              radialDimension.displayValue = lines.Select(l => LineToSpeckle(l) as ICurve).ToList();

            textPoint = new Point3d(o.Plane.OriginX + o.TextPosition.X, o.Plane.OriginZ + o.TextPosition.Y, o.Plane.OriginZ);
            props = Utilities.GetApplicationProps(o, typeof(RadialDimension), true, ignore);
            _dimension = radialDimension;
          }
          break;
      }

      if (_dimension != null && props != null)
      {
        // set text values
        _dimension.value = dimension.PlainText;
        _dimension.richText = dimension.RichText;
        _dimension.textPosition = PointToSpeckle(textPoint);

        // set rhino props
        var style = dimension.DimensionStyle.HasName ? dimension.DimensionStyle.Name : String.Empty;
        if (!string.IsNullOrEmpty(style)) props["DimensionStyleName"] = style;
        props["plane"] = PlaneToSpeckle(dimension.Plane);
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
      var plane = sourceAppProps["plane"] as Plane != null ? PlaneToNative(sourceAppProps["plane"] as Plane) : new Rhino.Geometry.Plane(position, Vector3d.ZAxis);

      string dimensionStyleName = sourceAppProps["DimensionStyleName"] as string;
      DimensionStyle dimensionStyle = Doc.DimStyles.FindName(dimensionStyleName) ?? Doc.DimStyles.Current;

      string className = sourceAppProps != null ? sourceAppProps["class"] as string : string.Empty;
      switch (className)
      {
        case "LinearDimension":
          DistanceDimension linearDimension = dimension as DistanceDimension;
          var start = PointToNative(linearDimension.measured[0]).Location;
          var end = PointToNative(linearDimension.measured[1]).Location;
          bool isRotated = sourceAppProps["AnnotationType"] as string == AnnotationType.Rotated.ToString() ? true : false;
          if (isRotated)
            _dimension = LinearDimension.Create(AnnotationType.Rotated, dimensionStyle, plane, Vector3d.XAxis, start, end, position, 0);
          else
            _dimension = LinearDimension.Create(AnnotationType.Aligned, dimensionStyle, plane, Vector3d.XAxis, start, end, position, 0);
          Utilities.SetApplicationProps(_dimension, typeof(LinearDimension), sourceAppProps);
          break;
        case "AngularDimension":
          AngleDimension angleDimension = dimension as AngleDimension;
          if (angleDimension.measured.Count < 2) return null;
          var angularCenter = PointToNative(angleDimension.measured[0].start).Location;
          var angularStart = PointToNative(angleDimension.measured[0].end).Location;
          var angularEnd = PointToNative(angleDimension.measured[1].end).Location;
          _dimension = AngularDimension.Create(dimensionStyle, plane, Vector3d.XAxis, angularCenter, angularStart, angularEnd, position);
          Utilities.SetApplicationProps(_dimension, typeof(AngularDimension), sourceAppProps);
          break;
        case "OrdinateDimension":
          var ordinateSpeckle = dimension as DistanceDimension;
          if (ordinateSpeckle == null || ordinateSpeckle.measured.Count < 2 || ordinateSpeckle.direction == null) return null;
          var ordinateBase = PointToNative(ordinateSpeckle.measured[0]).Location;
          var ordinateDefining = PointToNative(ordinateSpeckle.measured[1]).Location;
          var kinkOffset1 = sourceAppProps["KinkOffset1"] as double? ?? 0;
          var kinkOffset2 = sourceAppProps["KinkOffset2"] as double? ?? 0;
          bool isXDirection = VectorToNative(ordinateSpeckle.direction).IsParallelTo(Vector3d.XAxis) == 0 ? false : true;
          if (isXDirection)
            _dimension = OrdinateDimension.Create(dimensionStyle, plane, OrdinateDimension.MeasuredDirection.Xaxis, ordinateBase, ordinateDefining, position, kinkOffset1, kinkOffset2);
          else
            _dimension = OrdinateDimension.Create(dimensionStyle, plane, OrdinateDimension.MeasuredDirection.Yaxis, ordinateBase, ordinateDefining, position, kinkOffset1, kinkOffset2);
          Utilities.SetApplicationProps(_dimension, typeof(OrdinateDimension), sourceAppProps);
          break;
        case "RadialDimension":
          var radialSpeckle = dimension as LengthDimension;
          if (radialSpeckle == null || radialSpeckle.measured as Line == null) return null;
          var radialLine = LineToNative(radialSpeckle.measured as Line);
          _dimension = RadialDimension.Create(dimensionStyle, AnnotationType.Radius, plane, radialLine.PointAtStart, radialLine.PointAtEnd, position);
          Utilities.SetApplicationProps(_dimension, typeof(RadialDimension), sourceAppProps);
          break;
        default:
          _dimension = DimensionToNative(dimension);
          break;
      }

      _dimension.DimensionStyleId = dimensionStyle.Id;
      var textPosition = PointToNative(dimension.textPosition).Location;
      _dimension.TextPosition = new Point2d(textPosition.X - _dimension.Plane.OriginX, textPosition.Y - _dimension.Plane.OriginY);
      return _dimension;
    }

    public Rhino.Geometry.Dimension DimensionToNative(Dimension dimension)
    {
      Rhino.Geometry.Dimension _dimension = null;
      var style = Doc.DimStyles.Current;
      var position = PointToNative(dimension.position).Location;
      var plane = new Rhino.Geometry.Plane(position, Vector3d.ZAxis);

      switch (dimension)
      {
        case LengthDimension o:
          switch (o.measured)
          {
            case Line l:
              var radialLine = LineToNative(l);
              var radialDimension = RadialDimension.Create(style, AnnotationType.Radius, plane, radialLine.PointAtStart, radialLine.PointAtEnd, position);
              _dimension = radialDimension;
              break;
            default: // all other curve length types will have to return a generic annotation
              break;
          }
          break;
        case AngleDimension o:
          if (o.measured.Count < 2) return null;

          var angularCenter = PointToNative(o.measured[0].start).Location;
          var angularStart = PointToNative(o.measured[0].end).Location;
          var angularEnd = PointToNative(o.measured[1].end).Location;
          _dimension = AngularDimension.Create(style, plane, Vector3d.XAxis, angularCenter, angularStart, angularEnd, position);
          break;
        case DistanceDimension o:
          if (o.measured.Count < 2) return null;
          var start = PointToNative(o.measured[0]).Location;
          var end = PointToNative(o.measured[1]).Location;
          var normal = VectorToNative(o.direction);
          if (o.isOrdinate)
          {
            bool isXDirection = normal.IsParallelTo(Vector3d.XAxis) == 0 ? false : true;
            if (isXDirection)
              _dimension = OrdinateDimension.Create(style, plane, OrdinateDimension.MeasuredDirection.Xaxis, start, end, position, 0, 0);
            else
              _dimension = OrdinateDimension.Create(style, plane, OrdinateDimension.MeasuredDirection.Yaxis, start, end, position, 0, 0);
          }
          else
          {
            var dir = new Vector3d(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
            if (normal.IsPerpendicularTo(dir))
              _dimension = LinearDimension.Create(AnnotationType.Aligned, style, plane, Vector3d.XAxis, start, end, position, 0);
            else
            {
              dir.Rotate(Math.PI / 2, Vector3d.ZAxis);
              var rotationAngle = Vector3d.VectorAngle(dir, normal);
              _dimension = LinearDimension.Create(AnnotationType.Rotated, style, plane, Vector3d.XAxis, start, end, position, rotationAngle);
            }
          }
          break;
        default:
          break;
      }
      if (_dimension != null)
      {
        // set text properties
        _dimension.PlainText = dimension.value;
        if (!string.IsNullOrEmpty(dimension.richText)) _dimension.RichText = dimension.richText;
        var textPosition = PointToNative(dimension.textPosition).Location;
        _dimension.TextPosition = new Point2d(textPosition.X - _dimension.Plane.OriginX, textPosition.Y - _dimension.Plane.OriginY);
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
