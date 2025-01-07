using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Windows.Data;
using Objects.BuiltElements.Revit;
using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.GraphTraversal;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;
using Arc = Objects.Geometry.Arc;
using BlockDefinition = Objects.Other.BlockDefinition;
using BlockInstance = Objects.Other.BlockInstance;
using Dimension = Objects.Other.Dimension;
using Hatch = Objects.Other.Hatch;
using HatchLoop = Objects.Other.HatchLoop;
using HatchLoopType = Objects.Other.HatchLoopType;
using Line = Objects.Geometry.Line;
using Point = Objects.Geometry.Point;
using Text = Objects.Other.Text;
using Utilities = Speckle.Core.Models.Utilities;

namespace Objects.Converter.AutocadCivil;

public partial class ConverterAutocadCivil
{
  // Layers
  public Collection LayerToSpeckle(LayerTableRecord layer)
  {
    var collection = new Collection(layer.Name, "layer") { applicationId = layer.Id.ToString() };

    // add dynamic autocad props
    DisplayStyle style = new() { color = layer.Color.ColorValue.ToArgb(), units = Units.Millimeters };
    var linetype = (LinetypeTableRecord)Trans.GetObject(layer.LinetypeObjectId, OpenMode.ForRead);
    style.linetype = linetype.Name;
    var lineWeight =
      (layer.LineWeight == LineWeight.ByLineWeightDefault || layer.LineWeight == LineWeight.ByBlock)
        ? (int)LineWeight.LineWeight025
        : (int)layer.LineWeight;
    style.lineweight = lineWeight / 100; // convert to mm
    collection["displayStyle"] = style;

    collection["visible"] = !layer.IsHidden;

    return collection;
  }

  public ApplicationObject CollectionToNative(Collection collection)
  {
    var appObj = new ApplicationObject(collection.id, collection.speckle_type)
    {
      applicationId = collection.applicationId
    };

    ApplicationObject.State status = ApplicationObject.State.Unknown;
    LayerTableRecord layer = null;

    if (collection["path"] is string layerPath)
    {
      // see if this layer already exists in the doc
      LayerTableRecord existingLayer = GetLayer(layerPath);

      // update this layer if it exists & receive mode is on update
      if (existingLayer != null)
      {
        if (ReceiveMode == ReceiveMode.Update)
        {
          layer = existingLayer;
          status = ApplicationObject.State.Updated;
        }
        else
        {
          layerPath += $" - {DateTime.Now.ToString()}";
        }
      }

      // otherwise, create this layer
      if (layer == null)
      {
        if (MakeLayer(layerPath, out layer))
        {
          status = ApplicationObject.State.Created;
        }
        else
        {
          appObj.Update(status: ApplicationObject.State.Failed, logItem: "Could not create layer");
          return appObj;
        }
      }

      // get attributes
      Base styleBase = collection["displayStyle"] is DisplayStyle displayStyle
        ? displayStyle
        : collection["renderMaterial"] is RenderMaterial renderMaterial
          ? renderMaterial
          : null;
      if (styleBase is not null)
      {
        DisplayStyleToNative(
          styleBase,
          out Color color,
          out Transparency transparency,
          out LineWeight lineWeight,
          out ObjectId lineType
        );

        if (!layer.IsWriteEnabled)
        {
          layer.UpgradeOpen();
        }

        layer.Color = color;
        layer.Transparency = transparency;
        layer.LineWeight = lineWeight;
        layer.LinetypeObjectId = lineType;
      }

      appObj.Update(status: status, convertedItem: layer, createdId: layer.Id.ToString());
    }
    else
    {
      appObj.Update(status: ApplicationObject.State.Failed, logItem: "Layer path did not exist on Collection.");
    }

    return appObj;
  }

  // Display Style
  private static LineWeight GetLineWeight(double weight)
  {
    double hundredthMM = weight * 100;
    var weights = Enum.GetValues(typeof(LineWeight)).Cast<int>().ToList();
    int closest = weights.Aggregate((x, y) => Math.Abs(x - hundredthMM) < Math.Abs(y - hundredthMM) ? x : y);
    return (LineWeight)closest;
  }

  public void DisplayStyleToNative(
    Base styleBase,
    out Color color,
    out Transparency transparency,
    out LineWeight lineWeight,
    out ObjectId lineType
  )
  {
    var systemColor = new System.Drawing.Color();
    byte alpha = 255;
    lineWeight = LineWeight.ByLineWeightDefault;
    lineType = LineTypeDictionary.First().Value;
    if (styleBase is DisplayStyle style)
    {
      systemColor = System.Drawing.Color.FromArgb(style.color);
      alpha = systemColor.A;

      double conversionFactor =
        (style.units != null) ? Units.GetConversionFactor(Units.GetUnitsFromString(style.units), Units.Millimeters) : 1;
      lineWeight = GetLineWeight(style.lineweight * conversionFactor);

      if (LineTypeDictionary.ContainsKey(style.linetype))
      {
        lineType = LineTypeDictionary[style.linetype];
      }
    }
    else if (styleBase is RenderMaterial material) // this is the fallback value if a rendermaterial is passed instead
    {
      systemColor = System.Drawing.Color.FromArgb(material.diffuse);
      alpha = (byte)(material.opacity * 255d);
    }
    color = Color.FromRgb(systemColor.R, systemColor.G, systemColor.B);
    transparency = new Transparency(alpha);
  }

  public DisplayStyle DisplayStyleToSpeckle(Entity entity)
  {
    if (entity is null)
    {
      return null;
    }

    var style = new DisplayStyle();

    // get color
    int color = System.Drawing.Color.Black.ToArgb();
    switch (entity.Color.ColorMethod)
    {
      case ColorMethod.ByLayer:
        using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
        {
          if (entity.LayerId.IsValid)
          {
            var layer = tr.GetObject(entity.LayerId, OpenMode.ForRead) as LayerTableRecord;
            color = layer.Color.ColorValue.ToArgb();
          }
          tr.Commit();
        }
        break;
      case ColorMethod.ByBlock:
      case ColorMethod.ByAci:
      case ColorMethod.ByColor:
        color = entity.Color.ColorValue.ToArgb();
        break;
      default:
        break;
    }
    style.color = color;

    // get linetype
    style.linetype = entity.Linetype;
    if (entity.Linetype.ToUpper() == "BYLAYER")
    {
      using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
      {
        if (entity.LayerId.IsValid)
        {
          var layer = tr.GetObject(entity.LayerId, OpenMode.ForRead) as LayerTableRecord;
          var linetype = (LinetypeTableRecord)tr.GetObject(layer.LinetypeObjectId, OpenMode.ForRead);
          style.linetype = linetype.Name;
        }
        tr.Commit();
      }
    }

    // get lineweight
    // system variable default is: LWDEFAULT
    double lineWeight = 0.25;
    switch (entity.LineWeight)
    {
      case LineWeight.ByLayer:
        using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
        {
          if (entity.LayerId.IsValid)
          {
            var layer = tr.GetObject(entity.LayerId, OpenMode.ForRead) as LayerTableRecord;
            lineWeight =
              layer.LineWeight == LineWeight.ByLineWeightDefault || layer.LineWeight == LineWeight.ByBlock
                ? (int)LineWeight.LineWeight025
                : (int)layer.LineWeight;
          }
          tr.Commit();
        }
        break;
      case LineWeight.ByBlock:
      case LineWeight.ByLineWeightDefault:
      case LineWeight.ByDIPs:
        lineWeight = (int)LineWeight.LineWeight025;
        break;
      default:
        lineWeight = (int)entity.LineWeight;
        break;
    }
    style.lineweight = lineWeight / 100; // convert to mm

    style.units = Units.Millimeters;

    return style;
  }

  // Hatches
  private HatchLoopType HatchLoopTypeToSpeckle(HatchLoopTypes type)
  {
    if (type.HasFlag(HatchLoopTypes.Outermost) || type.HasFlag(HatchLoopTypes.External))
    {
      return HatchLoopType.Outer;
    }

    if (type.HasFlag(HatchLoopTypes.Default))
    {
      return HatchLoopType.Unknown;
    }

    return HatchLoopType.Unknown;
  }

  private HatchLoopTypes HatchLoopTypeToNative(HatchLoopType type)
  {
    switch (type)
    {
      case HatchLoopType.Outer:
        return HatchLoopTypes.External;
      default:
        return HatchLoopTypes.Default;
    }
  }

  public Hatch HatchToSpeckle(AcadDB.Hatch hatch)
  {
    var _hatch = new Hatch
    {
      pattern = hatch.PatternName,
      scale = hatch.PatternScale,
      rotation = hatch.PatternAngle
    };

    // handle curves
    var curves = new List<HatchLoop>();
    for (int i = 0; i < hatch.NumberOfLoops; i++)
    {
      var loop = hatch.GetLoopAt(i);
      if (loop.IsPolyline)
      {
        var poly = GetPolylineFromBulgeVertexCollection(loop.Polyline);
        var convertedPoly = poly.IsOnlyLines ? PolylineToSpeckle(poly) : PolycurveToSpeckle(poly);
        var speckleLoop = new HatchLoop(convertedPoly, HatchLoopTypeToSpeckle(loop.LoopType));
        curves.Add(speckleLoop);
      }
      else
      {
        for (int j = 0; j < loop.Curves.Count; j++)
        {
          var convertedCurve = CurveToSpeckle(loop.Curves[j]);
          var speckleLoop = new HatchLoop(convertedCurve, HatchLoopTypeToSpeckle(loop.LoopType));
          curves.Add(speckleLoop);
        }
      }
    }
    _hatch.loops = curves;
    _hatch["style"] = hatch.HatchStyle.ToString();

    return _hatch;
  }

  // TODO: this needs to be improved, hatch curves not being created with HatchLoopTypes.Polyline flag
  public ApplicationObject HatchToNativeDB(Hatch hatch)
  {
    var appObj = new ApplicationObject(hatch.id, hatch.speckle_type) { applicationId = hatch.applicationId };

    BlockTableRecord modelSpaceRecord = Doc.Database.GetModelSpace();

    // convert curves
    var loops = new Dictionary<AcadDB.Curve, HatchLoopTypes>();
    if (hatch.loops != null)
    {
      foreach (var loop in hatch.loops)
      {
        var converted = CurveToNativeDB(loop.Curve);
        if (converted == null || converted.Count == 0)
        {
          appObj.Log.Add($"Could not create {loop.Type} loop {loop.id}");
          continue;
        }
        foreach (var convertedItem in converted)
        {
          var curveId = modelSpaceRecord.Append(convertedItem);
          if (curveId.IsValid)
          {
            HatchLoopTypes type = HatchLoopTypeToNative(loop.Type);
            loops.Add(convertedItem, type);
          }
          else
          {
            appObj.Log.Add($"Could not add {loop.Type} loop {loop.id} to model space");
          }
        }
      }
    }

    if (loops.Count == 0)
    {
      throw new ConversionException("No loops were successfully created");
    }

    // add hatch to modelspace
    var newHatch = new AcadDB.Hatch();
    modelSpaceRecord.Append(newHatch);

    newHatch.SetDatabaseDefaults();

    // try get hatch pattern
    var patternCategory = HatchPatterns.ValidPatternName(hatch.pattern);
    switch (patternCategory)
    {
      case PatPatternCategory.kCustomdef:
        newHatch.SetHatchPattern(HatchPatternType.CustomDefined, hatch.pattern);
        break;
      case PatPatternCategory.kPredef:
      case PatPatternCategory.kISOdef:
        newHatch.SetHatchPattern(HatchPatternType.PreDefined, hatch.pattern);
        break;
      case PatPatternCategory.kUserdef:
        newHatch.SetHatchPattern(HatchPatternType.UserDefined, hatch.pattern);
        break;
      default:
        newHatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
        break;
    }
    newHatch.PatternAngle = hatch.rotation;
    newHatch.PatternScale = hatch.scale;

    if (hatch["style"] is string style)
    {
      newHatch.HatchStyle = Enum.TryParse(style, out HatchStyle hatchStyle) ? hatchStyle : HatchStyle.Normal;
    }

    // create loops
    foreach (var entry in loops)
    {
      var loopHandle = entry.Key.Handle.ToString();
      try
      {
        newHatch.AppendLoop(entry.Value, new ObjectIdCollection() { entry.Key.ObjectId });
        newHatch.EvaluateHatch(true);
        entry.Key.Erase(); // delete created hatch loop curve
      }
      catch (Exception e) when (!e.IsFatal())
      {
        // A hatch loop failed to create, but potentially can still create the rest of the hatch.
        appObj.Update(
          createdId: loopHandle,
          convertedItem: entry.Key,
          logItem: $"Could not append loop {loopHandle}: {e.Message}"
        );
      }
    }

    return appObj;
  }

  private AcadDB.Polyline GetPolylineFromBulgeVertexCollection(BulgeVertexCollection bulges)
  {
    var polyline = new AcadDB.Polyline(bulges.Count);
    double totalBulge = 0;
    for (int i = 0; i < bulges.Count; i++)
    {
      BulgeVertex bulgeVertex = bulges[i];
      polyline.AddVertexAt(i, bulgeVertex.Vertex, bulgeVertex.Bulge, 1.0, 1.0);
      totalBulge += bulgeVertex.Bulge;
    }
    polyline.Closed = bulges[0].Vertex.IsEqualTo(bulges[bulges.Count - 1].Vertex);
    return polyline;
  }

  // Blocks
  public BlockInstance BlockReferenceToSpeckle(BlockReference reference)
  {
    // get record
    BlockDefinition definition;
    var attributes = new Dictionary<string, string>();

    var btrObjId = reference.BlockTableRecord;
    if (reference.IsDynamicBlock)
    {
      btrObjId =
        reference.AnonymousBlockTableRecord != ObjectId.Null
          ? reference.AnonymousBlockTableRecord
          : reference.DynamicBlockTableRecord;
    }

    var btr = (BlockTableRecord)Trans.GetObject(btrObjId, OpenMode.ForRead);
    definition = BlockRecordToSpeckle(btr);
    foreach (ObjectId id in reference.AttributeCollection)
    {
      AttributeReference attRef = (AttributeReference)Trans.GetObject(id, OpenMode.ForRead);
      attributes.Add(attRef.Tag, attRef.TextString);
    }

    if (definition == null)
    {
      throw new ConversionException("Could not convert definition.");
    }

    var instance = new BlockInstance()
    {
      transform = new Transform(reference.BlockTransform.ToArray(), ModelUnits),
      typedDefinition = definition,
      units = ModelUnits
    };

    // add attributes
    if (attributes.Count != 0)
    {
      instance["attributes"] = attributes;
    }

    return instance;
  }

  public ApplicationObject InstanceToNativeDB(Instance instance, bool AppendToModelSpace = true)
  {
    var appObj = new ApplicationObject(instance.id, instance.speckle_type) { applicationId = instance.applicationId };

    // convert the definition
    var definition = instance.definition ?? instance["@definition"] as Base ?? instance["@blockDefinition"] as Base; // some applications need to dynamically attach defs (eg sketchup)
    if (definition == null)
    {
      throw new ConversionException("Instance did not have a definition");
    }

    ObjectId definitionId = DefinitionToNativeDB(definition, out List<string> notes);
    if (notes.Count > 0)
    {
      appObj.Update(log: notes);
      Report.UpdateReportObject(appObj);
    }

    if (definitionId == ObjectId.Null)
    {
      throw new ConversionException("Could not convert instance definition.");
    }

    // delete existing objs if any and this is an update
    if (ReceiveMode == ReceiveMode.Update)
    {
      List<ObjectId> existingObjs = GetExistingElementsByApplicationId(instance.applicationId);

      foreach (ObjectId existingObjId in existingObjs)
      {
        var existingObj = Trans.GetObject(existingObjId, OpenMode.ForWrite);
        if (!existingObj.IsErased)
        {
          existingObj.Erase();
        }
      }
    }

    // transform
    Matrix3d convertedTransform = TransformToNativeMatrix(instance.transform);

    // add block reference
    BlockTableRecord modelSpaceRecord = Doc.Database.GetModelSpace();
    var insertionPoint = Point3d.Origin.TransformBy(convertedTransform);
    BlockReference br = new(insertionPoint, definitionId) { BlockTransform = convertedTransform };

    // add attributes if there are any
    if (instance["attributes"] is Dictionary<string, object> attributes)
    {
      // TODO: figure out how to add attributes
    }
    ObjectId id = ObjectId.Null;
    if (AppendToModelSpace)
    {
      id = modelSpaceRecord.Append(br);
    }

    if ((!id.IsValid || id.IsNull) && AppendToModelSpace)
    {
      throw new ConversionException("Couldn't append instance to model space");
    }

    // update appobj
    var status = ReceiveMode == ReceiveMode.Update ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
    appObj.Update(status: status, convertedItem: br);
    if (AppendToModelSpace)
    {
      appObj.CreatedIds.Add(id.Handle.ToString());
    }

    return appObj;
  }

  public BlockDefinition BlockRecordToSpeckle(BlockTableRecord record)
  {
    // get geometry
    var geometry = new List<Base>();
    foreach (ObjectId id in record)
    {
      DBObject obj = Trans.GetObject(id, OpenMode.ForRead);
      Entity objEntity = obj as Entity;
      if (CanConvertToSpeckle(obj) && objEntity != null && objEntity.Visible)
      {
        Base converted = ConvertToSpeckle(obj);
        if (converted != null)
        {
          converted["layer"] = objEntity.Layer;
          geometry.Add(converted);
        }
      }
    }

    var definition = new BlockDefinition()
    {
      name = GetBlockDefName(record),
      basePoint = PointToSpeckle(record.Origin),
      geometry = geometry,
      units = ModelUnits
    };

    return definition;
  }

  public ObjectId DefinitionToNativeDB(Base definition, out List<string> notes)
  {
    notes = new List<string>();

    // get the definition name
    var commitInfo = RemoveInvalidChars(Doc.UserData["commit"] as string);
    string definitionName = definition is BlockDefinition blockDef
      ? RemoveInvalidChars(blockDef.name)
      : definition is RevitSymbolElementType revitDef
        ? RemoveInvalidChars($"{revitDef.family} - {revitDef.type} - {definition.id}")
        : definition.id;
    if (ReceiveMode == ReceiveMode.Create)
    {
      definitionName = $"{commitInfo} - " + definitionName;
    }

    BlockTable blckTbl = Trans.GetObject(Doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
    if (blckTbl.Has(definitionName))
    {
      return blckTbl[definitionName];
    }

    // get definition geometry to traverse and base point
    Point3d basePoint = Point3d.Origin;
    var toTraverse = new List<Base>();
    switch (definition)
    {
      case BlockDefinition o:
        if (o.basePoint != null)
        {
          basePoint = PointToNative(o.basePoint);
        }

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
    ObjectIdCollection bakedGeometry = new(); // this is to contain block def geometry that is already added to doc space during conversion

    var converted = new List<Entity>();
    foreach (var item in conversionDict)
    {
      var geo = item.Key;
      var convertedGeo = new List<Entity>();
      DisplayStyle style = geo["displayStyle"] as DisplayStyle;
      RenderMaterial material = geo["renderMaterial"] as RenderMaterial;

      switch (geo)
      {
        case Instance o:
          var instanceNotes = new List<string>();
          var instanceAppObj = InstanceToNativeDB(o, false);
          var instance = instanceAppObj.Converted.FirstOrDefault() as BlockReference;
          if (instance != null)
          {
            convertedGeo.Add(instance);
          }
          else
          {
            notes.AddRange(instanceNotes);
            notes.Add($"Could not create nested Instance of definition {definitionName}");
          }
          break;
        default:
          ConvertWithDisplay(geo, style, material, ref convertedGeo);
          break;
      }
      if (convertedGeo.Count == 0)
      {
        notes.Add($"Could not create definition geometry {geo.speckle_type} ({geo.id})");
        continue;
      }

      foreach (var convertedItem in convertedGeo)
      {
        if (!convertedItem.IsNewObject && convertedItem is not BlockReference)
        {
          bakedGeometry.Add(convertedItem.Id);
        }
        else
        {
          converted.Add(convertedItem);
        }
      }
    }

    if (converted.Count == 0 && bakedGeometry.Count == 0)
    {
      notes.Add("Could not convert any definition geometry");
      return ObjectId.Null;
    }

    // create btr
    ObjectId blockId = ObjectId.Null;
    using (BlockTableRecord btr = new())
    {
      btr.Name = definitionName;
      btr.Origin = basePoint;

      // add geometry
      blckTbl.UpgradeOpen();
      foreach (var convertedItem in converted)
      {
        btr.AppendEntity(convertedItem);
      }
      blockId = blckTbl.Add(btr);
      btr.AssumeOwnershipOf(bakedGeometry); // add in baked geo
      Trans.AddNewlyCreatedDBObject(btr, true);
      blckTbl.Dispose();
    }

    return blockId;
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
        GraphTraversal.TraverseMember(fallbackMember).ToList().ForEach(o => StoreObject(o, containerId));
        return;
      }
    }

    string LayerId(TraversalContext context) => LayerIdRecurse(context, new StringBuilder()).ToString();
    StringBuilder LayerIdRecurse(TraversalContext context, StringBuilder stringBuilder)
    {
      if (context.propName == null)
      {
        return stringBuilder;
      }

      // see if there's a layer property on this obj
      var layer = context.current["layer"] as string ?? context.current["Layer"] as string;
      if (!string.IsNullOrEmpty(layer))
      {
        return new StringBuilder(layer);
      }

      var objectLayerName = context.propName[0] == '@' ? context.propName.Substring(1) : context.propName;

      LayerIdRecurse(context.parent, stringBuilder);
      stringBuilder.Append("$");
      stringBuilder.Append(objectLayerName);

      return stringBuilder;
    }

    var traverseFunction = DefaultTraversal.CreateTraverseFunc(this);

    traverseFunction.Traverse(obj).ToList().ForEach(tc => StoreObject(tc.current, LayerId(tc)));

    return StoredObjects;
  }
  #endregion

  /// <summary>
  /// Get the name of the block definition from BlockTableRecord.
  /// If btr is a Dynamic Block, name is formatted as "DynamicBlockName"_"VisibilityName"
  /// </summary>
  /// <param name="btr">BlockTableRecord object</param>
  /// <returns>block table record name</returns>
  private string GetBlockDefName(BlockTableRecord btr)
  {
    var fullName = btr.Name;
    var curVisibilityName = string.Empty;

    if (btr.IsAnonymous || btr.IsDynamicBlock)
    {
      var referenceIds = btr.GetBlockReferenceIds(true, false);
      ObjectId referenceId = referenceIds.Count > 0 ? referenceIds[0] : ObjectId.Null;
      BlockReference reference =
        referenceId != ObjectId.Null ? Trans.GetObject(referenceId, OpenMode.ForRead) as BlockReference : null;
      if (reference == null)
      {
        return fullName;
      }

      if (btr.IsAnonymous)
      {
        BlockTableRecord dynamicBlock =
          reference.DynamicBlockTableRecord != ObjectId.Null
            ? Trans.GetObject(reference.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord
            : null;
        if (dynamicBlock != null)
        {
          fullName = dynamicBlock.Name;
        }
      }

      var descriptiveProps = new List<string>();
      foreach (DynamicBlockReferenceProperty prop in reference.DynamicBlockReferencePropertyCollection)
      {
        if (prop.VisibleInCurrentVisibilityState && !prop.ReadOnly && IsSimpleType(prop.Value, out string value))
        {
          descriptiveProps.Add(value);
        }
      }

      if (descriptiveProps.Count > 0)
      {
        fullName = $"{fullName}_{string.Join("_", descriptiveProps.ToArray())}";
      }
    }

    return fullName;
  }

  private bool IsSimpleType(object value, out string stringValue)
  {
    stringValue = string.Empty;
    var type = value.GetType();
    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
    {
      // nullable type, check if the nested type is simple.
      return IsSimpleType(type.GetGenericArguments()[0], out stringValue);
    }
    if (type.IsPrimitive || type.IsEnum || type.Equals(typeof(string)) || type.Equals(typeof(decimal)))
    {
      stringValue = value.ToString();
      return true;
    }
    return false;
  }

  private void ConvertWithDisplay(
    Base obj,
    DisplayStyle style,
    RenderMaterial material,
    ref List<Entity> converted,
    bool TryUseObjDisplay = false
  )
  {
    if (TryUseObjDisplay && obj["displayStyle"] as DisplayStyle != null)
    {
      style = obj["displayStyle"] as DisplayStyle;
    }

    if (TryUseObjDisplay && obj["renderMaterial"] as RenderMaterial != null)
    {
      material = obj["renderMaterial"] as RenderMaterial;
    }

    var convertedList = new List<object>();
    var convertedGeo = ConvertToNative(obj);
    if (convertedGeo == null)
    {
      return;
    }

    //Iteratively flatten any lists
    void FlattenConvertedObject(object item)
    {
      if (item is System.Collections.IList list)
      {
        foreach (object child in list)
        {
          FlattenConvertedObject(child);
        }
      }
      else
      {
        convertedList.Add(item);
      }
    }
    FlattenConvertedObject(convertedGeo);

    foreach (Entity entity in convertedList.Cast<Entity>())
    {
      if (entity != null)
      {
        // get display attributes
        Base styleBase = style != null ? style : material;
        DisplayStyleToNative(
          styleBase,
          out Color color,
          out Transparency transparency,
          out LineWeight lineWeight,
          out ObjectId lineType
        );
        entity.Color = color;
        entity.Transparency = transparency;
        entity.LineWeight = lineWeight;
        entity.LinetypeId = lineType;

        converted.Add(entity);
      }
    }
  }

  // Text
  public Text TextToSpeckle(DBText text)
  {
    var _text = new Text
    {
      // not realistically feasible to extract outline curves for displayvalue currently
      height = text.Height
    };
    var center = GetTextCenter(text);
    _text.plane = PlaneToSpeckle(new Plane(center, text.Normal));
    _text.rotation = text.Rotation;
    _text.value = text.TextString;
    _text.units = ModelUnits;

    // autocad props
    var excludeProps = new List<string>() { "Height", "Rotation", "TextString" };
    Base props = Utilities.GetApplicationProps(text, typeof(DBText), true, excludeProps);
    props["TextPosition"] = PointToSpeckle(text.Position);
    _text[AutocadPropName] = props;

    return _text;
  }

  public Text TextToSpeckle(MText text)
  {
    var _text = new Text
    {
      // not realistically feasible to extract outline curves for displayvalue currently
      height = text.Height == 0 ? text.ActualHeight : text.Height
    };
    var center = (text.Bounds != null) ? GetTextCenter(text.Bounds.Value) : text.Location;
    _text.plane = PlaneToSpeckle(new Plane(center, text.Normal));
    _text.rotation = text.Rotation;
    _text.value = text.Text;
    _text.richText = text.ContentsRTF;
    _text.units = ModelUnits;

    // autocad props
    var excludeProps = new List<string>() { "Height", "Rotation", "Contents", "ContentsRTF" };
    Base props = Utilities.GetApplicationProps(text, typeof(MText), true, excludeProps);
    props["TextPosition"] = PointToSpeckle(text.Location);
    _text[AutocadPropName] = props;

    return _text;
  }

  public Entity AcadTextToNative(Text text)
  {
    if (text[AutocadPropName] is not Base sourceAppProps)
    {
      return TextToNative(text);
    }

    Point textPosition =
      sourceAppProps["TextPosition"] != null ? sourceAppProps["TextPosition"] as Point : text.plane.origin;
    ObjectId textStyle =
      sourceAppProps["TextStyleName"] != null
        ? GetTextStyle(sourceAppProps["TextStyleName"] as string)
        : Doc.Database.Textstyle;

    string className = sourceAppProps["class"] as string;
    Entity _text;
    switch (className)
    {
      case "MText":
        MText mText = TextToNative(text);
        mText.Location = PointToNative(textPosition);
        mText.TextStyleId = textStyle;
        Utilities.SetApplicationProps(mText, typeof(MText), sourceAppProps);
        _text = mText;
        break;
      case "DBText":
        var dbText = new DBText
        {
          TextString = text.value,
          Height = ScaleToNative(text.height, text.units),
          Position = PointToNative(textPosition),
          Rotation = text.rotation,
          Normal = VectorToNative(text.plane.normal),
          TextStyleId = textStyle
        };
        Utilities.SetApplicationProps(dbText, typeof(DBText), sourceAppProps);
        _text = dbText;
        break;
      default:
        _text = TextToNative(text);
        break;
    }
    return _text;
  }

  public MText TextToNative(Text text)
  {
    var _text = new MText();

    if (string.IsNullOrEmpty(text.richText))
    {
      _text.Contents = text.value;
    }
    else
    {
      _text.ContentsRTF = text.richText;
    }

    _text.TextHeight = ScaleToNative(text.height, text.units);
    _text.Location = PointToNative(text.plane.origin);
    _text.Rotation = text.rotation;
    _text.Normal = VectorToNative(text.plane.normal);

    return _text;
  }

  private ObjectId GetTextStyle(string styleName)
  {
    var textStyleTable = Trans.GetObject(Doc.Database.TextStyleTableId, OpenMode.ForRead) as DimStyleTable;
    foreach (ObjectId id in textStyleTable)
    {
      var textStyle = Trans.GetObject(id, OpenMode.ForRead) as DimStyleTableRecord;
      if (textStyle.Name == styleName)
      {
        return id;
      }
    }
    return ObjectId.Null;
  }

  private Point3d GetTextCenter(Extents3d extents)
  {
    var x = (extents.MaxPoint.X + extents.MinPoint.X) / 2.0;
    var y = (extents.MaxPoint.Y + extents.MinPoint.Y) / 2.0;
    var z = (extents.MaxPoint.Z + extents.MinPoint.Z) / 2.0;

    return new Point3d(x, y, z);
  }

  private Point3d GetTextCenter(DBText text)
  {
    var position = text.Position;
    double x = position.X;
    double y = position.Y;
    double z = position.Z;

    if (text.Bounds != null)
    {
      var extents = text.Bounds.Value;
      x = (extents.MaxPoint.X + extents.MinPoint.X) / 2.0;
      y = (extents.MaxPoint.Y + extents.MinPoint.Y) / 2.0;
      z = (extents.MaxPoint.Z + extents.MinPoint.Z) / 2.0;

      return new Point3d(x, y, z);
    }

    var alignment = text.AlignmentPoint;
    var height = text.Height;
    switch (text.Justify)
    {
      case AttachmentPoint.BottomMid:
      case AttachmentPoint.BottomCenter:
        x = alignment.X;
        y = alignment.Y + height / 2;
        break;
      case AttachmentPoint.TopCenter:
      case AttachmentPoint.TopMid:
        x = alignment.X;
        y = alignment.Y - height / 2;
        break;
      case AttachmentPoint.MiddleRight:
        x = alignment.X - (alignment.X - position.X) / 2;
        y = alignment.Y;
        break;
      case AttachmentPoint.BottomRight:
        x = alignment.X - (alignment.X - position.X) / 2;
        y = alignment.Y + height / 2;
        break;
      case AttachmentPoint.TopRight:
        x = alignment.X - (alignment.X - position.X) / 2;
        y = alignment.Y - height / 2;
        break;
      case AttachmentPoint.MiddleCenter:
      case AttachmentPoint.MiddleMid:
        x = alignment.X;
        y = alignment.Y;
        break;
      default:
        break;
    }
    return new Point3d(x, y, z);
  }

  // Dimension
  public Dimension DimensionToSpeckle(AcadDB.Dimension dimension)
  {
    Dimension _dimension = null;
    Base props = null;
    var ignore = new List<string>() { "DimensionText", "Measurement" };

    switch (dimension)
    {
      case AlignedDimension o:
        var alignedDimension = new DistanceDimension()
        {
          units = ModelUnits,
          value = dimension.DimensionText,
          measurement = dimension.Measurement,
          isOrdinate = false
        };
        var alignedNormal = new Vector3d(
          o.XLine2Point.X - o.XLine1Point.X,
          o.XLine2Point.Y - o.XLine1Point.Y,
          o.XLine2Point.Z - o.XLine1Point.Z
        ).GetPerpendicularVector();
        alignedDimension.direction = VectorToSpeckle(alignedNormal);
        alignedDimension.position = PointToSpeckle(o.DimLinePoint);
        alignedDimension.measured = new List<Point>() { PointToSpeckle(o.XLine1Point), PointToSpeckle(o.XLine2Point) };
        props = Utilities.GetApplicationProps(o, typeof(AlignedDimension), true, ignore);
        _dimension = alignedDimension;
        break;
      case RotatedDimension o:
        var rotatedDimension = new DistanceDimension()
        {
          units = ModelUnits,
          value = dimension.DimensionText,
          measurement = dimension.Measurement,
          isOrdinate = false
        };
        var rotatedNormal = new Vector3d(
          o.XLine2Point.X - o.XLine1Point.X,
          o.XLine2Point.Y - o.XLine1Point.Y,
          o.XLine2Point.Z - o.XLine1Point.Z
        ).GetPerpendicularVector();
        rotatedDimension.direction = VectorToSpeckle(rotatedNormal.RotateBy(o.Rotation, Vector3d.ZAxis));
        rotatedDimension.position = PointToSpeckle(o.DimLinePoint);
        rotatedDimension.measured = new List<Point>() { PointToSpeckle(o.XLine1Point), PointToSpeckle(o.XLine2Point) };
        props = Utilities.GetApplicationProps(o, typeof(RotatedDimension), true, ignore);
        _dimension = rotatedDimension;
        break;
      case OrdinateDimension o:
        var ordinateDimension = new DistanceDimension
        {
          units = ModelUnits,
          value = dimension.DimensionText,
          measurement = dimension.Measurement,
          isOrdinate = true,
          direction = o.UsingXAxis ? VectorToSpeckle(Vector3d.XAxis) : VectorToSpeckle(Vector3d.YAxis),
          position = PointToSpeckle(o.LeaderEndPoint),
          measured = new List<Point>() { PointToSpeckle(o.Origin), PointToSpeckle(o.DefiningPoint) }
        };
        props = Utilities.GetApplicationProps(o, typeof(OrdinateDimension), true, ignore);
        _dimension = ordinateDimension;
        break;
      case RadialDimension o:
        var radialDimension = new LengthDimension
        {
          units = ModelUnits,
          value = dimension.DimensionText,
          measurement = dimension.Measurement,
          measured = new Line(PointToSpeckle(o.Center), PointToSpeckle(o.ChordPoint), ModelUnits),
          position = PointToSpeckle(o.ChordPoint) // TODO: the position could be improved by using the leader length x the direction of the dimension
        };
        props = Utilities.GetApplicationProps(o, typeof(RadialDimension), true, ignore);
        _dimension = radialDimension;
        break;
      case DiametricDimension o:
        var diametricDimension = new LengthDimension
        {
          units = ModelUnits,
          value = dimension.DimensionText,
          measurement = dimension.Measurement,
          measured = new Line(PointToSpeckle(o.FarChordPoint), PointToSpeckle(o.ChordPoint), ModelUnits),
          position = PointToSpeckle(o.ChordPoint) // TODO: the position could be improved by using the leader length x the direction of the dimension
        };
        props = Utilities.GetApplicationProps(o, typeof(DiametricDimension), true, ignore);
        _dimension = diametricDimension;
        break;
      case ArcDimension o:
        var arcDimension = new LengthDimension
        {
          units = ModelUnits,
          value = dimension.DimensionText,
          measurement = dimension.Measurement,
          measured = ArcToSpeckle(new CircularArc3d(o.XLine1Point, o.ArcPoint, o.XLine2Point)),
          position = PointToSpeckle(o.ArcPoint)
        };
        props = Utilities.GetApplicationProps(o, typeof(ArcDimension), true, ignore);
        _dimension = arcDimension;
        break;
      case LineAngularDimension2 o:
        var lineAngularDimension = new AngleDimension()
        {
          units = ModelUnits,
          value = dimension.DimensionText,
          measurement = dimension.Measurement
        };
        var line1 = new Line(PointToSpeckle(o.XLine1Start), PointToSpeckle(o.XLine1End), ModelUnits);
        var line2 = new Line(PointToSpeckle(o.XLine2Start), PointToSpeckle(o.XLine2End), ModelUnits);
        lineAngularDimension.measured = new List<Line>() { line1, line2 };
        lineAngularDimension.position = PointToSpeckle(o.ArcPoint);
        props = Utilities.GetApplicationProps(o, typeof(LineAngularDimension2), true, ignore);
        _dimension = lineAngularDimension;
        break;
      case Point3AngularDimension o:
        var pointAngularDimension = new AngleDimension()
        {
          units = ModelUnits,
          value = dimension.DimensionText,
          measurement = dimension.Measurement
        };
        var point1 = new Line(PointToSpeckle(o.ArcPoint), PointToSpeckle(o.XLine1Point), ModelUnits);
        var point2 = new Line(PointToSpeckle(o.ArcPoint), PointToSpeckle(o.XLine2Point), ModelUnits);
        pointAngularDimension.measured = new List<Line>() { point1, point2 };
        pointAngularDimension.position = PointToSpeckle(o.ArcPoint);
        props = Utilities.GetApplicationProps(o, typeof(Point3AngularDimension), true, ignore);
        _dimension = pointAngularDimension;
        break;
    }
    if (_dimension != null && props != null)
    {
      _dimension.textPosition = PointToSpeckle(dimension.TextPosition);
      _dimension[AutocadPropName] = props;
    }
    return _dimension;
  }

  public AcadDB.Dimension AcadDimensionToNative(Dimension dimension)
  {
    AcadDB.Dimension _dimension = null;
    if (dimension[AutocadPropName] is not Base sourceAppProps)
    {
      return DimensionToNative(dimension);
    }

    ObjectId dimensionStyle =
      sourceAppProps["DimensionStyleName"] != null
        ? GetDimensionStyle(sourceAppProps["DimensionStyleName"] as string)
        : Doc.Database.Dimstyle;
    if (dimensionStyle == ObjectId.Null)
    {
      dimensionStyle = Doc.Database.Dimstyle;
    }

    Point3d position = PointToNative(dimension.position);
    string className = sourceAppProps["class"] as string;
    switch (className)
    {
      case "AlignedDimension":
        if (dimension is not DistanceDimension alignedSpeckle || alignedSpeckle.measured.Count < 2)
        {
          throw new ConversionException(
            "Aligned dimension was not a DistanceDimension or measured count was less than 2"
          );
        }

        Point3d alignedStart = PointToNative(alignedSpeckle.measured[0]);
        Point3d alignedEnd = PointToNative(alignedSpeckle.measured[1]);
        var alignedDimension = new AlignedDimension(
          alignedStart,
          alignedEnd,
          position,
          dimension.value,
          dimensionStyle
        );
        Utilities.SetApplicationProps(alignedDimension, typeof(AlignedDimension), sourceAppProps);
        _dimension = alignedDimension;
        break;
      case "RotatedDimension":
        if (dimension is not DistanceDimension rotatedSpeckle || rotatedSpeckle.measured.Count < 2)
        {
          throw new ConversionException(
            "Rotated dimension was not a DistanceDimension or measured count was less than 2"
          );
        }

        double rotation = sourceAppProps["Rotation"] as double? ?? 0;
        Point3d rotatedStart = PointToNative(rotatedSpeckle.measured[0]);
        Point3d rotatedEnd = PointToNative(rotatedSpeckle.measured[1]);

        var rotatedDimension = new RotatedDimension(
          rotation,
          rotatedStart,
          rotatedEnd,
          position,
          dimension.value,
          dimensionStyle
        );
        Utilities.SetApplicationProps(rotatedDimension, typeof(RotatedDimension), sourceAppProps);
        _dimension = rotatedDimension;

        break;
      case "OrdinateDimension":
        var ordinateDimension = DimensionToNative(dimension) as OrdinateDimension;
        if (ordinateDimension != null)
        {
          Utilities.SetApplicationProps(ordinateDimension, typeof(OrdinateDimension), sourceAppProps);
        }

        _dimension = ordinateDimension;
        break;
      case "RadialDimension":
        var radialDimension = DimensionToNative(dimension) as RadialDimension;
        if (radialDimension != null)
        {
          Utilities.SetApplicationProps(radialDimension, typeof(RadialDimension), sourceAppProps);
        }

        _dimension = radialDimension;

        break;
      case "DiametricDimension":
        if (dimension is not LengthDimension diametricSpeckle || diametricSpeckle.measured as Line == null)
        {
          throw new ConversionException(
            "Diametric dimension was not a LengthDimension or measured value was not a Line"
          );
        }

        var line = diametricSpeckle.measured as Line;
        Point3d start = PointToNative(line.start);
        Point3d end = PointToNative(line.end);
        double leaderLength = ScaleToNative(sourceAppProps["LeaderLength"] as double? ?? 0, ModelUnits);
        var diametricDimension = new DiametricDimension(end, start, leaderLength, dimension.value, dimensionStyle);
        sourceAppProps["LeaderLength"] = leaderLength;
        Utilities.SetApplicationProps(diametricDimension, typeof(DiametricDimension), sourceAppProps);
        _dimension = diametricDimension;
        break;
      case "ArcDimension":
        if (dimension is not LengthDimension arcSpeckle || arcSpeckle.measured as Arc == null)
        {
          throw new ConversionException("Arc dimension was not a LengthDimension or measured value was not an Arc");
        }

        CircularArc3d arc = ArcToNative(arcSpeckle.measured as Arc);
        if (arc != null)
        {
          var arcDimension = new ArcDimension(
            arc.Center,
            arc.StartPoint,
            arc.EndPoint,
            position,
            dimension.value,
            dimensionStyle
          );
          Utilities.SetApplicationProps(arcDimension, typeof(ArcDimension), sourceAppProps);
          _dimension = arcDimension;
        }

        break;
      case "LineAngularDimension2":
        var lineAngularDimension = DimensionToNative(dimension) as LineAngularDimension2;
        if (lineAngularDimension != null)
        {
          Utilities.SetApplicationProps(lineAngularDimension, typeof(LineAngularDimension2), sourceAppProps);
        }

        _dimension = lineAngularDimension;
        break;
      case "Point3AngularDimension":
        var pointAngularDimension = DimensionToNative(dimension) as Point3AngularDimension;
        if (pointAngularDimension != null)
        {
          Utilities.SetApplicationProps(pointAngularDimension, typeof(Point3AngularDimension), sourceAppProps);
        }

        _dimension = pointAngularDimension;
        break;
      default:
        _dimension = DimensionToNative(dimension);
        break;
    }
    if (_dimension != null)
    {
      _dimension.TextPosition = PointToNative(dimension.textPosition);
      _dimension.DimensionStyle = dimensionStyle;
    }
    return _dimension;
  }

  public AcadDB.Dimension DimensionToNative(Dimension dimension)
  {
    if (dimension.position == null)
    {
      throw new ConversionException("Position was null");
    }

    AcadDB.Dimension autocadDimension = null;
    var style = Doc.Database.Dimstyle;
    Point3d position = PointToNative(dimension.position);
    string value = dimension.value ?? "";
    switch (dimension)
    {
      case LengthDimension o:
        switch (o.measured)
        {
          case Arc a:
            if (a.plane?.origin == null || a.startPoint == null || a.endPoint == null)
            {
              throw new ConversionException("Arc did not have a plane origin or start point or end point.");
            }

            Point3d arcCenter = PointToNative(a.plane.origin);
            Point3d arcStart = PointToNative(a.startPoint);
            Point3d arcEnd = PointToNative(a.endPoint);
            var arcDimension = new ArcDimension(arcCenter, arcStart, arcEnd, position, value, style);
            autocadDimension = arcDimension;
            break;
          case Line l:
            Point3d radialStart = PointToNative(l.start);
            Point3d radialEnd = PointToNative(l.end);
            double leaderLength = radialEnd.DistanceTo(position);
            var radialDimension = new RadialDimension(radialStart, radialEnd, leaderLength, value, style);
            autocadDimension = radialDimension;
            break;
        }
        break;
      case AngleDimension o:

        if (o.measured.Count < 2)
        {
          throw new ConversionException("Angle dimension had a measured count of less than 2.");
        }

        Point3d line1Start = PointToNative(o.measured[0].start);
        Point3d line1End = PointToNative(o.measured[0].end);
        Point3d line2Start = PointToNative(o.measured[1].start);
        Point3d line2End = PointToNative(o.measured[1].end);
        autocadDimension =
          Math.Round(line1Start.DistanceTo(line2Start), 3) == 0
            ? new Point3AngularDimension(line1Start, line1End, line2End, position, value, style)
            : new LineAngularDimension2(line1Start, line1End, line2Start, line2End, position, value, style);
        break;
      case DistanceDimension o:
        if (o.measured.Count < 2 || o.direction is null)
        {
          throw new ConversionException("Distance dimension had no direction or a measured count of less than 2.");
        }

        Point3d start = PointToNative(o.measured[0]);
        Point3d end = PointToNative(o.measured[1]);
        Vector3d normal = VectorToNative(o.direction);

        if (o.isOrdinate)
        {
          bool useXAxis = normal.IsParallelTo(Vector3d.XAxis);
          var ordinateDimension = new OrdinateDimension(useXAxis, end, position, dimension.value, style)
          {
            Origin = start
          };
          autocadDimension = ordinateDimension;
        }
        else
        {
          var dir = new Vector3d(end.X - start.X, end.Y - start.Y, end.Z - start.Z); // dimension direction
          var angleBetween = Math.Round(dir.GetAngleTo(normal), 3);
          autocadDimension = dir.IsParallelTo(normal, Tolerance.Global)
            ? new AlignedDimension(start, end, position, dimension.value, style)
            : new RotatedDimension(angleBetween, start, end, position, dimension.value, style);
        }

        break;
      default:
        break;
    }

    return autocadDimension;
  }

  private ObjectId GetDimensionStyle(string styleName)
  {
    var dimStyleTable = Trans.GetObject(Doc.Database.DimStyleTableId, OpenMode.ForRead) as DimStyleTable;
    foreach (ObjectId id in dimStyleTable)
    {
      var dimStyle = Trans.GetObject(id, OpenMode.ForRead) as DimStyleTableRecord;
      if (dimStyle.Name == styleName)
      {
        return id;
      }
    }
    return ObjectId.Null;
  }

  // Proxy Entity
  public Base ProxyEntityToSpeckle(ProxyEntity proxy)
  {
    // Currently not possible to retrieve geometry of proxy entities, so sending props as a base instead
    var _proxy = new Base();
    _proxy["bbox"] = BoxToSpeckle(proxy.GeometricExtents);
    var props = Utilities.GetApplicationProps(proxy, typeof(ProxyEntity), false);
    props["ApplicationDescription"] = proxy.ApplicationDescription;
    props["OriginalClassName"] = proxy.OriginalClassName;
    props["OriginalDxfName"] = proxy.OriginalDxfName;
    props["ProxyFlags"] = proxy.ProxyFlags;
    if (props != null)
    {
      _proxy[AutocadPropName] = props;
    }

    return _proxy;
  }
}
