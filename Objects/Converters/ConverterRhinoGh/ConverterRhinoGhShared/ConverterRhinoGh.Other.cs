using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Objects.BuiltElements.Revit;
using Objects.Other;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Render;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using Dimension = Objects.Other.Dimension;
using Hatch = Rhino.Geometry.Hatch;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using RenderMaterial = Rhino.Render.RenderMaterial;
using RH = Rhino.DocObjects;
using Transform = Rhino.Geometry.Transform;
using Utilities = Speckle.Core.Models.Utilities;

namespace Objects.Converter.RhinoGh;

public partial class ConverterRhinoGh
{
  // display, render
  public RH.ObjectAttributes DisplayStyleToNative(DisplayStyle display)
  {
    var attributes = new RH.ObjectAttributes();

    // color
    attributes.ObjectColor = Color.FromArgb(display.color);
    var colorSource = RH.ObjectColorSource.ColorFromObject;
    if (display["colorSource"] != null)
      Enum.TryParse(display["colorSource"] as string, out colorSource);
    attributes.ColorSource = colorSource;

    // line type
    var lineStyle = Doc.Linetypes.FindName(display.linetype);
    attributes.LinetypeIndex = lineStyle != null ? lineStyle.Index : 0;
    var lineSource = RH.ObjectLinetypeSource.LinetypeFromObject;
    if (display["lineSource"] != null)
      Enum.TryParse(display["lineSource"] as string, out lineSource);
    attributes.LinetypeSource = lineSource;

    // plot weight
    var conversionFactor =
      display.units == null ? 1 : Units.GetConversionFactor(Units.GetUnitsFromString(display.units), Units.Millimeters);
    attributes.PlotWeight = display.lineweight * conversionFactor;
    var weightSource = RH.ObjectPlotWeightSource.PlotWeightFromObject;
    if (display["weightSource"] != null)
      Enum.TryParse(display["weightSource"] as string, out weightSource);
    attributes.PlotWeightSource = weightSource;

    return attributes;
  }

  public DisplayStyle DisplayStyleToSpeckle(RH.ObjectAttributes attributes, RH.Layer layer = null)
  {
    var style = new DisplayStyle { units = Units.Millimeters };
    int color = Color.LightGray.ToArgb();
    RH.Linetype lineType = null;
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
        case RH.ObjectColorSource.ColorFromObject:
          color = attributes.ObjectColor.ToArgb();
          break;
        case RH.ObjectColorSource.ColorFromMaterial:
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
        case RH.ObjectLinetypeSource.LinetypeFromObject:
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
        case RH.ObjectPlotWeightSource.PlotWeightFromObject:
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
    if (colorSource != null)
      style["colorSource"] = colorSource;
    if (lineTypeSource != null)
      style["lineSource"] = lineTypeSource;
    if (weightSource != null)
      style["weightSource"] = weightSource;

    return style;
  }

  public RenderMaterial RenderMaterialToNative(Other.RenderMaterial speckleMaterial)
  {
    var commitInfo = GetCommitInfo();
    var speckleName =
      ReceiveMode == ReceiveMode.Create ? $"{commitInfo} - {speckleMaterial.name}" : $"{speckleMaterial.name}";

    // check if the doc already has a material with speckle material name, or a previously created speckle material
    //NOTE: Looking up renderMaterials this way is slow, maybe we can create a dictionary?
    var existing = Doc.RenderMaterials.FirstOrDefault(x => x.Name == speckleName);
    if (existing != null)
      return existing;

    RenderMaterial rm;
    //#if RHINO6
    var rhinoMaterial = new RH.Material
    {
      Name = speckleName,
      DiffuseColor = Color.FromArgb(speckleMaterial.diffuse),
      EmissionColor = Color.FromArgb(speckleMaterial.emissive),
      Transparency = 1 - speckleMaterial.opacity
    };
    Doc.Materials.Add(rhinoMaterial);
    rm = RenderMaterial.CreateBasicMaterial(rhinoMaterial, Doc);
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

  public Other.RenderMaterial RenderMaterialToSpeckle(RH.Material material)
  {
    var renderMaterial = new Other.RenderMaterial();
    if (material == null)
      return renderMaterial;

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
    if (!material.IsPhysicallyBased)
    {
      matToUse = new RH.Material();
      matToUse.CopyFrom(material);
      matToUse.ToPhysicallyBased();
    }
    using (var rm = RenderMaterial.FromMaterial(matToUse, null))
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

  public Other.RenderMaterial RenderMaterialToSpeckle(Rhino.Render.RenderMaterial material)
  {
    var renderMaterial = new Other.RenderMaterial();
    if (material == null)
      return renderMaterial;

    renderMaterial.name = material.Name ?? "default"; // default rhino material has no name or id
#if RHINO6
    var simulatedMaterial = material.SimulateMaterial(true);
    renderMaterial.diffuse = simulatedMaterial.DiffuseColor.ToArgb();
    renderMaterial.emissive = simulatedMaterial.EmissionColor.ToArgb();
    renderMaterial.opacity = 1 - simulatedMaterial.Transparency;

    // for some reason some default material transparency props are 1 when they shouldn't be - use this hack for now
    if ((renderMaterial.name.ToLower().Contains("glass") || renderMaterial.name.ToLower().Contains("gem")) && renderMaterial.opacity == 0)
      renderMaterial.opacity = 0.3;
#else
    RH.PhysicallyBasedMaterial pbrMaterial = material.ConvertToPhysicallyBased(RenderTexture.TextureGeneration.Allow);
    renderMaterial.diffuse = pbrMaterial.BaseColor.AsSystemColor().ToArgb();
    renderMaterial.emissive = pbrMaterial.Emission.AsSystemColor().ToArgb();
    renderMaterial.opacity = pbrMaterial.Opacity;
    renderMaterial.metalness = pbrMaterial.Metallic;
    renderMaterial.roughness = pbrMaterial.Roughness;

#endif

    return renderMaterial;
  }

  // hatch
  public Hatch[] HatchToNative(Other.Hatch hatch)
  {
    var curves = new List<Curve>();
    curves =
      hatch.loops != null
        ? hatch.loops.Select(o => CurveToNative(o.Curve)).ToList()
        : hatch.curves.Select(o => CurveToNative(o)).ToList();
    var pattern = Doc.HatchPatterns.FindName(hatch.pattern);
    int index;
    if (pattern == null)
    {
      // find default hatch pattern
      pattern = FindDefaultPattern(hatch.pattern);
      index = Doc.HatchPatterns.Add(pattern);
    }
    else
    {
      index = pattern.Index;
    }

    var hatches = Hatch.Create(curves, index, hatch.rotation, hatch.scale, 0.001);

    return hatches;
  }

  public Other.Hatch HatchToSpeckle(Hatch hatch)
  {
    var _hatch = new Other.Hatch();

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

  private RH.HatchPattern FindDefaultPattern(string patternName)
  {
    var defaultPattern = typeof(RH.HatchPattern.Defaults)
      .GetProperties()
      ?.Where(o => o.Name.Equals(patternName, StringComparison.OrdinalIgnoreCase))
      ?.ToList()
      .FirstOrDefault();
    if (defaultPattern != null)
      return defaultPattern.GetValue(this, null) as RH.HatchPattern;
    return RH.HatchPattern.Defaults.Solid;
  }

  // blocks
  public Transform TransformToNative(Other.Transform transform)
  {
    var matrix = transform.ConvertToUnits(ModelUnits);
    var _transform = Transform.Identity;
    double homogeneousDivisor = matrix[15]; // rhino doesn't seem to handle transform matrices where the translation vector last value is a divisor instead of 1, so make sure last value is set to 1
    int count = 0;
    for (var i = 0; i < 4; i++)
    {
      for (var j = 0; j < 4; j++)
      {
        _transform[i, j] = j == 3 && homogeneousDivisor != 1 ? matrix[count] / homogeneousDivisor : matrix[count];
        count++;
      }
    }

    return _transform;
  }

  public BlockDefinition BlockDefinitionToSpeckle(RH.InstanceDefinition definition)
  {
    // check if this has been converted and cached already
    if (BlockDefinitions.ContainsKey(definition.Name))
      return BlockDefinitions[definition.Name];

    var geometry = new List<Base>();
    foreach (var obj in definition.GetObjects())
      if (CanConvertToSpeckle(obj))
      {
        Base converted = ConvertToSpeckle(obj);
        if (converted != null)
        {
          converted["layer"] = Doc.Layers[obj.Attributes.LayerIndex].FullPath;
          geometry.Add(converted);
        }
      }

    // rhino by default sets selected block def base pt at world origin
    var _definition = new BlockDefinition(definition.Name, geometry, PointToSpeckle(Point3d.Origin))
    {
      units = ModelUnits,
      applicationId = definition.Id.ToString()
    };
    BlockDefinitions.Add(definition.Name, _definition);

    return _definition;
  }

  public RH.InstanceDefinition DefinitionToNative(Base definition, out List<string> notes)
  {
    notes = new List<string>();

    // get the definition name
    var commitInfo = GetCommitInfo();
    string definitionName = definition is BlockDefinition blockDef
      ? blockDef.name
      : definition is RevitSymbolElementType revitDef
        ? $"{revitDef.family} - {revitDef.type} - {definition.id}"
        : definition.id;
    if (ReceiveMode == ReceiveMode.Create)
      definitionName = $"{commitInfo} - " + definitionName;

    // check if this has been converted and cached already
    if (InstanceDefinitions.ContainsKey(definitionName))
      return InstanceDefinitions[definitionName];

    // update existing def of the same name if necessary
    var existingDef = Doc.InstanceDefinitions.Find(definitionName);
    if (existingDef is not null && ReceiveMode == ReceiveMode.Update)
    {
      if (!Doc.InstanceDefinitions.Delete(existingDef))
      {
        notes.Add($"Existing definition {definitionName} was not updated.");
        InstanceDefinitions.Add(definitionName, existingDef);
        return existingDef;
      }
    }

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
        if (!conversionDict.ContainsKey(key))
          conversionDict.Add(key, convertible[key]);
    }

    // convert definition geometry and attributes
    var converted = new List<GeometryBase>();
    var attributes = new List<RH.ObjectAttributes>();
    foreach (var item in conversionDict)
    {
      var geo = item.Key;
      var convertedGeo = new List<GeometryBase>();
      switch (geo)
      {
        case Instance o:
          var instanceNotes = new List<string>();
          var instanceAppObj = InstanceToNative(o, false);
          var instance = instanceAppObj.Converted.FirstOrDefault() as RH.InstanceObject;
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
      var attribute = new RH.ObjectAttributes();

      // layer
      var geoLayer = geo["layer"] is string s ? s : item.Value; // blocks sent from rhino will have a layer prop dynamically attached
      var layerName =
        ReceiveMode == ReceiveMode.Create ? $"{commitInfo}{RH.Layer.PathSeparator}{geoLayer}" : $"{geoLayer}";
      int index = 1;
      if (layerName != null)
        GetLayer(Doc, layerName, out index, true);
      attribute.LayerIndex = index;

      // display
      var renderMaterial = geo[@"renderMaterial"] as Other.RenderMaterial;
      if (geo[@"displayStyle"] is DisplayStyle display)
      {
        attribute = DisplayStyleToNative(display);
      }
      else if (renderMaterial != null)
      {
        attribute.ObjectColor = Color.FromArgb(renderMaterial.diffuse);
        attribute.ColorSource = RH.ObjectColorSource.ColorFromObject;
      }

      // render material
      if (renderMaterial != null)
      {
        var material = RenderMaterialToNative(renderMaterial);
        attribute.MaterialIndex = GetMaterialIndex(material?.Name);
        attribute.MaterialSource = RH.ObjectMaterialSource.MaterialFromObject;
      }

      converted.AddRange(convertedGeo);
      for (int i = 0; i < convertedGeo.Count; i++)
        attributes.Add(attribute);
    }

    if (converted.Count == 0)
    {
      notes.Add("Could not convert any definition geometry");
      return null;
    }

    // add definition to the doc, and instancedefinition cache
    int definitionIndex = Doc.InstanceDefinitions.Add(definitionName, string.Empty, basePoint, converted, attributes);
    if (definitionIndex < 0)
    {
      notes.Add("Could not add definition to the document");
      return null;
    }
    var blockDefinition = Doc.InstanceDefinitions[definitionIndex];
    InstanceDefinitions.Add(definitionName, blockDefinition);

    return blockDefinition;
  }

  // Rhino convention seems to order the origin of the vector space last instead of first
  // This results in a transposed transformation matrix - may need to be addressed later
  public BlockInstance BlockInstanceToSpeckle(RH.InstanceObject instance)
  {
    var t = instance.InstanceXform;
    var matrix = new System.DoubleNumerics.Matrix4x4(
      t.M00,
      t.M01,
      t.M02,
      t.M03,
      t.M10,
      t.M11,
      t.M12,
      t.M13,
      t.M20,
      t.M21,
      t.M22,
      t.M23,
      t.M30,
      t.M31,
      t.M32,
      t.M33
    );

    var def = BlockDefinitionToSpeckle(instance.InstanceDefinition);

    var _instance = new BlockInstance
    {
      transform = new Other.Transform(matrix, ModelUnits),
      typedDefinition = def,
      applicationId = instance.Id.ToString(),
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
    RH.InstanceDefinition instanceDef = DefinitionToNative(definition, out List<string> notes);
    if (notes.Count > 0)
      appObj.Update(log: notes);
    if (instanceDef == null)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Could not create block definition");
      return appObj;
    }

    // get the transform
    var transform = TransformToNative(instance.transform);

    // get any parameters
    var parameters = instance["parameters"] as Base;
    var attributes = new RH.ObjectAttributes();
    if (parameters != null)
    {
      foreach (var member in parameters.GetMembers(DynamicBaseMemberType.Dynamic))
      {
        if (member.Value is Parameter parameter)
        {
          var convertedParameter = ParameterToNative(parameter);
          var name = $"{convertedParameter.Item1}({member.Key})";
          attributes.SetUserString(name, convertedParameter.Item2);
        }
      }
    }

    // create the instance
    Guid instanceId = Doc.Objects.AddInstanceObject(instanceDef.Index, transform, attributes);

    if (instanceId == Guid.Empty)
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Could not add instance to doc");
      return appObj;
    }

    var _instance = Doc.Objects.FindId(instanceId) as RH.InstanceObject;

    // update appobj
    appObj.Update(convertedItem: _instance);
    if (AppendToModelSpace)
      appObj.CreatedIds.Add(instanceId.ToString());
    return appObj;
  }

  #region instance methods

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
        GraphTraversal.TraverseMember(fallbackMember).ToList().ForEach(o => StoreObject(o, containerId));
    }

    string LayerId(TraversalContext context) => LayerIdRecurse(context, new StringBuilder()).ToString();
    StringBuilder LayerIdRecurse(TraversalContext context, StringBuilder stringBuilder)
    {
      if (context.propName == null)
        return stringBuilder;

      // see if there's a layer property on this obj
      var layer = context.current["layer"] as string ?? context.current["Layer"] as string;
      if (!string.IsNullOrEmpty(layer))
        return new StringBuilder(layer);

      var objectLayerName = context.propName[0] == '@' ? context.propName.Substring(1) : context.propName;

      LayerIdRecurse(context.parent, stringBuilder);
      stringBuilder.Append(RH.Layer.PathSeparator);
      stringBuilder.Append(objectLayerName);

      return stringBuilder;
    }

    var traverseFunction = DefaultTraversal.CreateTraverseFunc(this);

    traverseFunction.Traverse(obj).ToList().ForEach(tc => StoreObject(tc.current, LayerId(tc)));

    return StoredObjects;
  }

  #endregion

  public DisplayMaterial RenderMaterialToDisplayMaterial(Other.RenderMaterial material)
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

  public Other.RenderMaterial DisplayMaterialToSpeckle(DisplayMaterial material)
  {
    var speckleMaterial = new Other.RenderMaterial();
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
      foreach (var outline in outlines)
      {
        Polyline poly = null;
        if (!outline.TryGetPolyline(out poly))
          outline.ToPolyline(0, 1, 0, 0, 0, 0.1, 0, 0, true).TryGetPolyline(out poly); // this is from nurbs, should probably be refined for text
        if (poly != null)
          _text.displayValue.Add(PolylineToSpeckle(poly) as Geometry.Polyline);
      }

    _text.plane = PlaneToSpeckle(text.Plane);
    _text.rotation = text.TextRotationRadians;
    _text.height = text.TextHeight * text.DimensionScale; // this needs to be multiplied by model space scale for true height
    _text.value = text.PlainText;
    _text.richText = text.RichText;
    _text.units = ModelUnits;

    // rhino props
    var ignore = new List<string> { "Text", "TextRotationRadians", "PlainText", "RichText", "FontIndex" };
    var props = Utilities.GetApplicationProps(text, typeof(TextEntity), true, ignore);
    var style = text.DimensionStyle.HasName ? text.DimensionStyle.Name : string.Empty;
    if (!string.IsNullOrEmpty(style))
      props["DimensionStyleName"] = style;
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
      var scaleProps = new List<string> { "TextHeight" };
      foreach (var scaleProp in scaleProps)
      {
        var value = sourceAppProps[scaleProp] as double?;
        if (value.HasValue)
          sourceAppProps[scaleProp] = ScaleToNative(value.Value, text.units);
      }
      Utilities.SetApplicationProps(_text, typeof(TextEntity), sourceAppProps);
      RH.DimensionStyle dimensionStyle = Doc.DimStyles.FindName(
        sourceAppProps["DimensionStyleName"] as string ?? string.Empty
      );
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
    var ignore = new List<string> { "Text", "PlainText", "RichText" };
    Point3d textPoint = new();

    switch (dimension)
    {
      case LinearDimension o:
        if (
          o.Get3dPoints(
            out Point3d linearStart,
            out Point3d linearEnd,
            out Point3d linearStartArrow,
            out Point3d linearEndArrow,
            out Point3d linearDimPoint,
            out textPoint
          )
        )
        {
          var linearDimension = new DistanceDimension
          {
            units = ModelUnits,
            measurement = dimension.NumericValue,
            isOrdinate = false
          };

          var normal = new Vector3d(
            linearEndArrow.X - linearStartArrow.X,
            linearEndArrow.Y - linearStartArrow.Y,
            linearEndArrow.Z - linearStartArrow.Z
          );
          normal.Rotate(Math.PI / 2, Vector3d.ZAxis);
          linearDimension.direction = VectorToSpeckle(normal);
          linearDimension.position = PointToSpeckle(linearDimPoint);
          linearDimension.measured = new List<Point> { PointToSpeckle(linearStart), PointToSpeckle(linearEnd) };
          if (o.GetDisplayLines(o.DimensionStyle, o.DimensionScale, out IEnumerable<Line> lines))
            linearDimension.displayValue = lines.Select(l => LineToSpeckle(l) as ICurve).ToList();

          props = Utilities.GetApplicationProps(o, typeof(LinearDimension), true, ignore);
          _dimension = linearDimension;
        }
        break;
      case AngularDimension o:
        if (
          o.Get3dPoints(
            out Point3d angularCenter,
            out Point3d angularStart,
            out Point3d angularEnd,
            out Point3d angularStartArrow,
            out Point3d angularEndArrow,
            out Point3d angularDimPoint,
            out textPoint
          )
        )
        {
          var lineStart = LineToSpeckle(new Line(angularCenter, angularStart));
          var lineEnd = LineToSpeckle(new Line(angularCenter, angularEnd));

          var angularDimension = new AngleDimension
          {
            units = ModelUnits,
            measurement = Math.PI / 180 * dimension.NumericValue
          };
          angularDimension.position = PointToSpeckle(angularDimPoint);
          angularDimension.measured = new List<Geometry.Line> { lineStart, lineEnd };
          if (o.GetDisplayLines(o.DimensionStyle, o.DimensionScale, out Line[] lines, out Arc[] arcs))
          {
            angularDimension.displayValue = lines.Select(l => LineToSpeckle(l) as ICurve).ToList();
            angularDimension.displayValue.AddRange(arcs.Select(a => ArcToSpeckle(a) as ICurve).ToList());
          }

          props = Utilities.GetApplicationProps(o, typeof(AngularDimension), true, ignore);
          _dimension = angularDimension;
        }
        break;
      case OrdinateDimension o:
        if (
          o.Get3dPoints(
            out Point3d basePoint,
            out Point3d ordinateDefPoint,
            out Point3d leader,
            out Point3d kink1Point,
            out Point3d kink2Point
          )
        )
        {
          var ordinateDimension = new DistanceDimension
          {
            units = ModelUnits,
            measurement = dimension.NumericValue,
            isOrdinate = true
          };
          ordinateDimension.direction =
            Math.Round(Math.Abs(ordinateDefPoint.X - basePoint.X) - o.NumericValue) == 0
              ? VectorToSpeckle(Vector3d.XAxis)
              : VectorToSpeckle(Vector3d.YAxis);
          ordinateDimension.position = PointToSpeckle(leader);
          ordinateDimension.measured = new List<Point> { PointToSpeckle(basePoint), PointToSpeckle(ordinateDefPoint) };
          if (o.GetDisplayLines(o.DimensionStyle, o.DimensionScale, out IEnumerable<Line> lines))
            ordinateDimension.displayValue = lines.Select(l => LineToSpeckle(l) as ICurve).ToList();
          textPoint = new Point3d(
            o.Plane.OriginX + o.TextPosition.X,
            o.Plane.OriginZ + o.TextPosition.Y,
            o.Plane.OriginZ
          );
          props = Utilities.GetApplicationProps(o, typeof(OrdinateDimension), true, ignore);
          _dimension = ordinateDimension;
        }
        break;
      case RadialDimension o:
        if (
          o.Get3dPoints(out Point3d radialCenter, out Point3d radius, out Point3d radialDimPoint, out Point3d kneePoint)
        )
        {
          var radialDimension = new LengthDimension { units = ModelUnits, measurement = dimension.NumericValue };
          radialDimension.position = PointToSpeckle(radialDimPoint);
          radialDimension.measured = LineToSpeckle(new Line(radialCenter, radius));
          if (o.GetDisplayLines(o.DimensionStyle, o.DimensionScale, out IEnumerable<Line> lines))
            radialDimension.displayValue = lines.Select(l => LineToSpeckle(l) as ICurve).ToList();

          textPoint = new Point3d(
            o.Plane.OriginX + o.TextPosition.X,
            o.Plane.OriginZ + o.TextPosition.Y,
            o.Plane.OriginZ
          );
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
      var style = dimension.DimensionStyle.HasName ? dimension.DimensionStyle.Name : string.Empty;
      if (!string.IsNullOrEmpty(style))
        props["DimensionStyleName"] = style;
      props["plane"] = PlaneToSpeckle(dimension.Plane);
      _dimension[RhinoPropName] = props;
    }
    return _dimension;
  }

  public Rhino.Geometry.Dimension RhinoDimensionToNative(Dimension dimension)
  {
    Rhino.Geometry.Dimension _dimension = null;
    Base sourceAppProps = dimension[RhinoPropName] as Base;
    if (sourceAppProps == null)
      return DimensionToNative(dimension);

    var position = PointToNative(dimension.position).Location;
    var plane =
      sourceAppProps["plane"] as Plane != null
        ? PlaneToNative(sourceAppProps["plane"] as Plane)
        : new Rhino.Geometry.Plane(position, Vector3d.ZAxis);

    string dimensionStyleName = sourceAppProps["DimensionStyleName"] as string;
    RH.DimensionStyle dimensionStyle = Doc.DimStyles.FindName(dimensionStyleName) ?? Doc.DimStyles.Current;

    string className = sourceAppProps != null ? sourceAppProps["class"] as string : string.Empty;
    switch (className)
    {
      case "LinearDimension":
        DistanceDimension linearDimension = dimension as DistanceDimension;
        var start = PointToNative(linearDimension.measured[0]).Location;
        var end = PointToNative(linearDimension.measured[1]).Location;
        bool isRotated = sourceAppProps["AnnotationType"] as string == AnnotationType.Rotated.ToString() ? true : false;
        if (isRotated)
          _dimension = LinearDimension.Create(
            AnnotationType.Rotated,
            dimensionStyle,
            plane,
            Vector3d.XAxis,
            start,
            end,
            position,
            0
          );
        else
          _dimension = LinearDimension.Create(
            AnnotationType.Aligned,
            dimensionStyle,
            plane,
            Vector3d.XAxis,
            start,
            end,
            position,
            0
          );
        Utilities.SetApplicationProps(_dimension, typeof(LinearDimension), sourceAppProps);
        break;
      case "AngularDimension":
        AngleDimension angleDimension = dimension as AngleDimension;
        if (angleDimension.measured.Count < 2)
          return null;
        var angularCenter = PointToNative(angleDimension.measured[0].start).Location;
        var angularStart = PointToNative(angleDimension.measured[0].end).Location;
        var angularEnd = PointToNative(angleDimension.measured[1].end).Location;
        _dimension = AngularDimension.Create(
          dimensionStyle,
          plane,
          Vector3d.XAxis,
          angularCenter,
          angularStart,
          angularEnd,
          position
        );
        Utilities.SetApplicationProps(_dimension, typeof(AngularDimension), sourceAppProps);
        break;
      case "OrdinateDimension":
        var ordinateSpeckle = dimension as DistanceDimension;
        if (ordinateSpeckle == null || ordinateSpeckle.measured.Count < 2 || ordinateSpeckle.direction == null)
          return null;
        var ordinateBase = PointToNative(ordinateSpeckle.measured[0]).Location;
        var ordinateDefining = PointToNative(ordinateSpeckle.measured[1]).Location;
        var kinkOffset1 = sourceAppProps["KinkOffset1"] as double? ?? 0;
        var kinkOffset2 = sourceAppProps["KinkOffset2"] as double? ?? 0;
        bool isXDirection = VectorToNative(ordinateSpeckle.direction).IsParallelTo(Vector3d.XAxis) == 0 ? false : true;
        if (isXDirection)
          _dimension = OrdinateDimension.Create(
            dimensionStyle,
            plane,
            OrdinateDimension.MeasuredDirection.Xaxis,
            ordinateBase,
            ordinateDefining,
            position,
            kinkOffset1,
            kinkOffset2
          );
        else
          _dimension = OrdinateDimension.Create(
            dimensionStyle,
            plane,
            OrdinateDimension.MeasuredDirection.Yaxis,
            ordinateBase,
            ordinateDefining,
            position,
            kinkOffset1,
            kinkOffset2
          );
        Utilities.SetApplicationProps(_dimension, typeof(OrdinateDimension), sourceAppProps);
        break;
      case "RadialDimension":
        var radialSpeckle = dimension as LengthDimension;
        if (radialSpeckle == null || radialSpeckle.measured as Geometry.Line == null)
          return null;
        var radialLine = LineToNative(radialSpeckle.measured as Geometry.Line);
        _dimension = RadialDimension.Create(
          dimensionStyle,
          AnnotationType.Radius,
          plane,
          radialLine.PointAtStart,
          radialLine.PointAtEnd,
          position
        );
        Utilities.SetApplicationProps(_dimension, typeof(RadialDimension), sourceAppProps);
        break;
      default:
        _dimension = DimensionToNative(dimension);
        break;
    }

    _dimension.DimensionStyleId = dimensionStyle.Id;
    var textPosition = PointToNative(dimension.textPosition).Location;
    _dimension.TextPosition = new Point2d(
      textPosition.X - _dimension.Plane.OriginX,
      textPosition.Y - _dimension.Plane.OriginY
    );
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
          case Geometry.Line l:
            var radialLine = LineToNative(l);
            var radialDimension = RadialDimension.Create(
              style,
              AnnotationType.Radius,
              plane,
              radialLine.PointAtStart,
              radialLine.PointAtEnd,
              position
            );
            _dimension = radialDimension;
            break;
        }
        break;
      case AngleDimension o:
        if (o.measured.Count < 2)
          return null;

        var angularCenter = PointToNative(o.measured[0].start).Location;
        var angularStart = PointToNative(o.measured[0].end).Location;
        var angularEnd = PointToNative(o.measured[1].end).Location;
        _dimension = AngularDimension.Create(
          style,
          plane,
          Vector3d.XAxis,
          angularCenter,
          angularStart,
          angularEnd,
          position
        );
        break;
      case DistanceDimension o:
        if (o.measured.Count < 2)
          return null;
        var start = PointToNative(o.measured[0]).Location;
        var end = PointToNative(o.measured[1]).Location;
        var normal = VectorToNative(o.direction);
        if (o.isOrdinate)
        {
          bool isXDirection = normal.IsParallelTo(Vector3d.XAxis) == 0 ? false : true;
          if (isXDirection)
            _dimension = OrdinateDimension.Create(
              style,
              plane,
              OrdinateDimension.MeasuredDirection.Xaxis,
              start,
              end,
              position,
              0,
              0
            );
          else
            _dimension = OrdinateDimension.Create(
              style,
              plane,
              OrdinateDimension.MeasuredDirection.Yaxis,
              start,
              end,
              position,
              0,
              0
            );
        }
        else
        {
          var dir = new Vector3d(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
          if (normal.IsPerpendicularTo(dir))
          {
            _dimension = LinearDimension.Create(
              AnnotationType.Aligned,
              style,
              plane,
              Vector3d.XAxis,
              start,
              end,
              position,
              0
            );
          }
          else
          {
            dir.Rotate(Math.PI / 2, Vector3d.ZAxis);
            var rotationAngle = Vector3d.VectorAngle(dir, normal);
            _dimension = LinearDimension.Create(
              AnnotationType.Rotated,
              style,
              plane,
              Vector3d.XAxis,
              start,
              end,
              position,
              rotationAngle
            );
          }
        }
        break;
    }
    if (_dimension != null)
    {
      // set text properties
      _dimension.PlainText = dimension.value;
      if (!string.IsNullOrEmpty(dimension.richText))
        _dimension.RichText = dimension.richText;
      var textPosition = PointToNative(dimension.textPosition).Location;
      _dimension.TextPosition = new Point2d(
        textPosition.X - _dimension.Plane.OriginX,
        textPosition.Y - _dimension.Plane.OriginY
      );
    }

    return _dimension;
  }

  public Color4f ARBGToColor4f(int argb)
  {
    var systemColor = Color.FromArgb(argb);
    return Color4f.FromArgb(systemColor.A, systemColor.R, systemColor.G, systemColor.B);
  }
}
