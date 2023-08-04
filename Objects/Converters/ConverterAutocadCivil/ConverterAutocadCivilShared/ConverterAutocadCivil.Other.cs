using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Windows.Data;
using Autodesk.AutoCAD.Colors;
using AcadBRep = Autodesk.AutoCAD.BoundaryRepresentation;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;

using Speckle.Core.Models;
using Speckle.Core.Kits;
using Speckle.Core.Models.GraphTraversal;
using Utilities = Speckle.Core.Models.Utilities;

using Objects.Other;
using Arc = Objects.Geometry.Arc;
using BlockInstance = Objects.Other.BlockInstance;
using BlockDefinition = Objects.Other.BlockDefinition;
using Dimension = Objects.Other.Dimension;
using Hatch = Objects.Other.Hatch;
using HatchLoop = Objects.Other.HatchLoop;
using HatchLoopType = Objects.Other.HatchLoopType;
using Line = Objects.Geometry.Line;
using Point = Objects.Geometry.Point;
using Text = Objects.Other.Text;
using Objects.BuiltElements.Revit;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    // Layers
    public Collection LayerToSpeckle(LayerTableRecord layer)
    {
      var collection = new Collection(layer.Name, "layer") { applicationId = layer.Id.ToString() };

      // add dynamic autocad props
      var style = new DisplayStyle() { units = Units.Millimeters };
      style.color = layer.Color.ColorValue.ToArgb();
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
      // get layer table
      var layerTable = (LayerTable)Trans.GetObject(Doc.Database.LayerTableId, OpenMode.ForWrite);

      #region local functions
      LayerTableRecord GetLayer(string path)
      {
        if (layerTable.Has(path))
          return (LayerTableRecord)Trans.GetObject(layerTable[path], OpenMode.ForWrite);
        return null;
      }
      LayerTableRecord MakeLayer(string name)
      {
        try
        {
          var _layer = new LayerTableRecord();

          // Assign the layer properties
          _layer.Name = name;

          // Append the new layer to the layer table and the transaction
          layerTable.Add(_layer);
          Trans.AddNewlyCreatedDBObject(_layer, true);

          return _layer;
        }
        catch (Exception e)
        {
          return null;
        }
      }
      #endregion

      var appObj = new ApplicationObject(collection.id, collection.speckle_type)
      {
        applicationId = collection.applicationId
      };
      LayerTableRecord layer = null;
      var status = ApplicationObject.State.Unknown;

      // see if this layer already exists in the doc
      var layerPath = collection["path"] as string;
      LayerTableRecord existingLayer = GetLayer(layerPath);

      // update this layer if it exists & receive mode is on update
      if (existingLayer != null && ReceiveMode == ReceiveMode.Update)
      {
        layer = existingLayer;
        status = ApplicationObject.State.Updated;
      }
      else // create this layer
      {
        layer = MakeLayer(layerPath);
        status = ApplicationObject.State.Created;
      }

      if (layer == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Could not create layer");
        return appObj;
      }

      // get attributes
      var displayStyle = collection["displayStyle"] as DisplayStyle;
      var renderMaterial = collection["renderMaterial"] as RenderMaterial;
      Base styleBase = displayStyle != null ? displayStyle : renderMaterial;
      DisplayStyleToNative(
        styleBase,
        out Color color,
        out Transparency transparency,
        out LineWeight lineWeight,
        out ObjectId lineType
      );
      layer.Color = color;
      layer.Transparency = transparency;
      layer.LineWeight = lineWeight;
      layer.LinetypeObjectId = lineType;

      appObj.Update(status: status, convertedItem: layer, createdId: layer.Id.ToString());
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
          (style.units != null)
            ? Units.GetConversionFactor(Units.GetUnitsFromString(style.units), Units.Millimeters)
            : 1;
        lineWeight = GetLineWeight(style.lineweight * conversionFactor);

        if (LineTypeDictionary.ContainsKey(style.linetype))
          lineType = LineTypeDictionary[style.linetype];
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
        return null;

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
              if (layer.LineWeight == LineWeight.ByLineWeightDefault || layer.LineWeight == LineWeight.ByBlock)
                lineWeight = (int)LineWeight.LineWeight025;
              else
                lineWeight = (int)layer.LineWeight;
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
        return HatchLoopType.Outer;
      if (type.HasFlag(HatchLoopTypes.Default))
        return HatchLoopType.Unknown;
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
      var _hatch = new Hatch();
      _hatch.pattern = hatch.PatternName;
      _hatch.scale = hatch.PatternScale;
      _hatch.rotation = hatch.PatternAngle;

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
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "No loops were successfully created");
        return appObj;
      }

      // add hatch to modelspace
      var _hatch = new AcadDB.Hatch();
      modelSpaceRecord.Append(_hatch);

      _hatch.SetDatabaseDefaults();

      // try get hatch pattern
      var patternCategory = HatchPatterns.ValidPatternName(hatch.pattern);
      switch (patternCategory)
      {
        case PatPatternCategory.kCustomdef:
          _hatch.SetHatchPattern(HatchPatternType.CustomDefined, hatch.pattern);
          break;
        case PatPatternCategory.kPredef:
        case PatPatternCategory.kISOdef:
          _hatch.SetHatchPattern(HatchPatternType.PreDefined, hatch.pattern);
          break;
        case PatPatternCategory.kUserdef:
          _hatch.SetHatchPattern(HatchPatternType.UserDefined, hatch.pattern);
          break;
        default:
          _hatch.SetHatchPattern(HatchPatternType.PreDefined, "SOLID");
          break;
      }
      _hatch.PatternAngle = hatch.rotation;
      _hatch.PatternScale = hatch.scale;

      var style = hatch["style"] as string;
      if (style != null)
        _hatch.HatchStyle = Enum.TryParse(style, out HatchStyle hatchStyle) ? hatchStyle : HatchStyle.Normal;

      // create loops
      foreach (var entry in loops)
      {
        var loopHandle = entry.Key.Handle.ToString();
        try
        {
          _hatch.AppendLoop(entry.Value, new ObjectIdCollection() { entry.Key.ObjectId });
          _hatch.EvaluateHatch(true);
          try
          {
            entry.Key.Erase(); // delete created hatch loop curve
          }
          catch (Exception e)
          {
            appObj.Update(
              createdId: loopHandle,
              convertedItem: entry.Key,
              logItem: $"Could not delete loop {loopHandle}: {e.Message}"
            );
          }
        }
        catch (Exception e)
        {
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
      polyline.Closed = bulges[0].Vertex.IsEqualTo(bulges[bulges.Count - 1].Vertex) ? true : false;
      return polyline;
    }

    // Blocks
    public BlockInstance BlockReferenceToSpeckle(BlockReference reference)
    {
      // get record
      BlockDefinition definition = null;
      var attributes = new Dictionary<string, string>();

      var btrObjId = reference.BlockTableRecord;
      if (reference.IsDynamicBlock)
        btrObjId =
          reference.AnonymousBlockTableRecord != ObjectId.Null
            ? reference.AnonymousBlockTableRecord
            : reference.DynamicBlockTableRecord;

      var btr = (BlockTableRecord)Trans.GetObject(btrObjId, OpenMode.ForRead);
      definition = BlockRecordToSpeckle(btr);
      foreach (ObjectId id in reference.AttributeCollection)
      {
        AttributeReference attRef = (AttributeReference)Trans.GetObject(id, OpenMode.ForRead);
        attributes.Add(attRef.Tag, attRef.TextString);
      }

      if (definition == null)
        return null;

      var instance = new BlockInstance()
      {
        transform = new Transform(reference.BlockTransform.ToArray(), ModelUnits),
        typedDefinition = definition,
        units = ModelUnits
      };

      // add attributes
      if (attributes.Any())
        instance["attributes"] = attributes;

      return instance;
    }

    public ApplicationObject InstanceToNativeDB(Instance instance, bool AppendToModelSpace = true)
    {
      var appObj = new ApplicationObject(instance.id, instance.speckle_type) { applicationId = instance.applicationId };

      // get the definition
      var definition = instance.definition ?? instance["@definition"] as Base ?? instance["@blockDefinition"] as Base; // some applications need to dynamically attach defs (eg sketchup)
      if (definition == null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "instance did not have a definition");
        return appObj;
      }

      // delete existing objs if any and this is an update
      if (ReceiveMode == ReceiveMode.Update)
      {
        var existingObjs = GetExistingElementsByApplicationId(instance.applicationId);
        try
        {
          foreach (var existingObjId in existingObjs)
          {
            var existingObj = Trans.GetObject(existingObjId, OpenMode.ForWrite);
            existingObj.Erase();
          }
        }
        catch (Exception e)
        {
          if (!e.Message.Contains("eWasErased")) // this couldve been previously deleted & received?
            appObj.Update(logItem: $"Could not remove one or more existing instances on update: {e.Message}");
        }
      }

      // convert the definition
      ObjectId definitionId = DefinitionToNativeDB(definition, out List<string> notes);
      if (notes.Count > 0)
        appObj.Update(log: notes);
      if (definitionId == ObjectId.Null)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Could not create block definition");
        return appObj;
      }

      // transform
      Matrix3d convertedTransform = TransformToNativeMatrix(instance.transform);

      // add block reference
      BlockTableRecord modelSpaceRecord = Doc.Database.GetModelSpace();
      var insertionPoint = Point3d.Origin.TransformBy(convertedTransform);
      BlockReference br = new BlockReference(insertionPoint, definitionId);
      br.BlockTransform = convertedTransform;

      // add attributes if there are any
      var attributes = instance["attributes"] as Dictionary<string, object>;
      if (attributes != null)
      {
        // TODO: figure out how to add attributes
      }
      ObjectId id = ObjectId.Null;
      if (AppendToModelSpace)
        id = modelSpaceRecord.Append(br);

      if ((!id.IsValid || id.IsNull) && AppendToModelSpace)
      {
        appObj.Update(status: ApplicationObject.State.Failed, logItem: "Couldn't append instance to model space");
        return appObj;
      }

      // update appobj
      var status =
        ReceiveMode == ReceiveMode.Update ? ApplicationObject.State.Updated : ApplicationObject.State.Created;
      appObj.Update(status: status, convertedItem: br);
      if (AppendToModelSpace)
        appObj.CreatedIds.Add(id.Handle.ToString());
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
        if (CanConvertToSpeckle(obj) && (objEntity != null && objEntity.Visible))
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
      var commitInfo = RemoveInvalidAutocadChars(Doc.UserData["commit"] as string);
      string definitionName = definition is BlockDefinition blockDef
        ? RemoveInvalidAutocadChars(blockDef.name)
        : definition is RevitSymbolElementType revitDef
          ? RemoveInvalidAutocadChars($"{revitDef.family} - {revitDef.type} - {definition.id}")
          : definition.id;
      if (ReceiveMode == ReceiveMode.Create)
        definitionName = $"{commitInfo} - " + definitionName;
      BlockTable blckTbl = Trans.GetObject(Doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
      if (blckTbl.Has(definitionName))
        return blckTbl[definitionName];

      // get definition geometry to traverse and base point
      Point3d basePoint = Point3d.Origin;
      var toTraverse = new List<Base>();
      switch (definition)
      {
        case BlockDefinition o:
          if (o.basePoint != null)
            basePoint = PointToNative(o.basePoint);
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
      var bakedGeometry = new ObjectIdCollection(); // this is to contain block def geometry that is already added to doc space during conversion

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
          if (!convertedItem.IsNewObject && !(convertedItem is BlockReference))
            bakedGeometry.Add(convertedItem.Id);
          else
            converted.Add(convertedItem);
        }
      }

      if (converted.Count == 0 && bakedGeometry.Count == 0)
      {
        notes.Add("Could not convert any definition geometry");
        return ObjectId.Null;
      }

      // create btr
      ObjectId blockId = ObjectId.Null;
      using (BlockTableRecord btr = new BlockTableRecord())
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
          return stringBuilder;

        // see if there's a layer property on this obj
        var layer = context.current["layer"] as string ?? context.current["Layer"] as string;
        if (!string.IsNullOrEmpty(layer))
          return new StringBuilder(layer);

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
          return fullName;

        if (btr.IsAnonymous)
        {
          BlockTableRecord dynamicBlock =
            reference.DynamicBlockTableRecord != ObjectId.Null
              ? Trans.GetObject(reference.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord
              : null;
          if (dynamicBlock != null)
            fullName = dynamicBlock.Name;
        }

        var descriptiveProps = new List<string>();
        foreach (DynamicBlockReferenceProperty prop in reference.DynamicBlockReferencePropertyCollection)
          if (prop.VisibleInCurrentVisibilityState && !prop.ReadOnly && IsSimpleType(prop.Value, out string value))
            descriptiveProps.Add(value);

        if (descriptiveProps.Count > 0)
          fullName = $"{fullName}_{String.Join("_", descriptiveProps.ToArray())}";
      }

      return fullName;
    }

    private bool IsSimpleType(object value, out string stringValue)
    {
      stringValue = String.Empty;
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
        style = obj["displayStyle"] as DisplayStyle;
      if (TryUseObjDisplay && obj["renderMaterial"] as RenderMaterial != null)
        material = obj["renderMaterial"] as RenderMaterial;

      var convertedList = new List<object>();
      var convertedGeo = ConvertToNative(obj);
      if (convertedGeo == null)
        return;

      //Iteratively flatten any lists
      void FlattenConvertedObject(object item)
      {
        if (item is System.Collections.IList list)
          foreach (object child in list)
            FlattenConvertedObject(child);
        else
          convertedList.Add(item);
      }
      FlattenConvertedObject(convertedGeo);

      foreach (Entity entity in convertedList)
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
      var _text = new Text();

      // not realistically feasible to extract outline curves for displayvalue currently
      _text.height = text.Height;
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
      var _text = new Text();

      // not realistically feasible to extract outline curves for displayvalue currently
      _text.height = text.Height == 0 ? text.ActualHeight : text.Height;
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
      Entity _text = null;
      Base sourceAppProps = text[AutocadPropName] as Base;
      if (sourceAppProps == null)
        return TextToNative(text);

      Point textPosition =
        sourceAppProps["TextPosition"] != null ? sourceAppProps["TextPosition"] as Point : text.plane.origin;
      ObjectId textStyle =
        sourceAppProps["TextStyleName"] != null
          ? GetTextStyle(sourceAppProps["TextStyleName"] as string)
          : Doc.Database.Textstyle;

      string className = sourceAppProps["class"] as string;
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
          var dbText = new DBText();
          dbText.TextString = text.value;
          dbText.Height = ScaleToNative(text.height, text.units);
          dbText.Position = PointToNative(textPosition);
          dbText.Rotation = text.rotation;
          dbText.Normal = VectorToNative(text.plane.normal);
          dbText.TextStyleId = textStyle;
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
        _text.Contents = text.value;
      else
        _text.ContentsRTF = text.richText;
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
          return id;
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
          y = alignment.Y + (height / 2);
          break;
        case AttachmentPoint.TopCenter:
        case AttachmentPoint.TopMid:
          x = alignment.X;
          y = alignment.Y - (height / 2);
          break;
        case AttachmentPoint.MiddleRight:
          x = alignment.X - ((alignment.X - position.X) / 2);
          y = alignment.Y;
          break;
        case AttachmentPoint.BottomRight:
          x = alignment.X - ((alignment.X - position.X) / 2);
          y = alignment.Y + (height / 2);
          break;
        case AttachmentPoint.TopRight:
          x = alignment.X - ((alignment.X - position.X) / 2);
          y = alignment.Y - (height / 2);
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
          alignedDimension.measured = new List<Point>()
          {
            PointToSpeckle(o.XLine1Point),
            PointToSpeckle(o.XLine2Point)
          };
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
          rotatedDimension.measured = new List<Point>()
          {
            PointToSpeckle(o.XLine1Point),
            PointToSpeckle(o.XLine2Point)
          };
          props = Utilities.GetApplicationProps(o, typeof(RotatedDimension), true, ignore);
          _dimension = rotatedDimension;
          break;
        case OrdinateDimension o:
          var ordinateDimension = new DistanceDimension()
          {
            units = ModelUnits,
            value = dimension.DimensionText,
            measurement = dimension.Measurement,
            isOrdinate = true
          };
          ordinateDimension.direction = o.UsingXAxis
            ? VectorToSpeckle(Vector3d.XAxis)
            : VectorToSpeckle(Vector3d.YAxis);
          ordinateDimension.position = PointToSpeckle(o.LeaderEndPoint);
          ordinateDimension.measured = new List<Point>() { PointToSpeckle(o.Origin), PointToSpeckle(o.DefiningPoint) };
          props = Utilities.GetApplicationProps(o, typeof(OrdinateDimension), true, ignore);
          _dimension = ordinateDimension;
          break;
        case RadialDimension o:
          var radialDimension = new LengthDimension()
          {
            units = ModelUnits,
            value = dimension.DimensionText,
            measurement = dimension.Measurement
          };
          radialDimension.measured = new Line(PointToSpeckle(o.Center), PointToSpeckle(o.ChordPoint), ModelUnits);
          radialDimension.position = PointToSpeckle(o.ChordPoint); // TODO: the position could be improved by using the leader length x the direction of the dimension
          props = Utilities.GetApplicationProps(o, typeof(RadialDimension), true, ignore);
          _dimension = radialDimension;
          break;
        case DiametricDimension o:
          var diametricDimension = new LengthDimension()
          {
            units = ModelUnits,
            value = dimension.DimensionText,
            measurement = dimension.Measurement
          };
          diametricDimension.measured = new Line(
            PointToSpeckle(o.FarChordPoint),
            PointToSpeckle(o.ChordPoint),
            ModelUnits
          );
          diametricDimension.position = PointToSpeckle(o.ChordPoint); // TODO: the position could be improved by using the leader length x the direction of the dimension
          props = Utilities.GetApplicationProps(o, typeof(DiametricDimension), true, ignore);
          _dimension = diametricDimension;
          break;
        case ArcDimension o:
          var arcDimension = new LengthDimension()
          {
            units = ModelUnits,
            value = dimension.DimensionText,
            measurement = dimension.Measurement
          };
          arcDimension.measured = ArcToSpeckle(new CircularArc3d(o.XLine1Point, o.ArcPoint, o.XLine2Point));
          arcDimension.position = PointToSpeckle(o.ArcPoint);
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
      Base sourceAppProps = dimension[AutocadPropName] as Base;
      if (sourceAppProps == null)
        return DimensionToNative(dimension);

      ObjectId dimensionStyle =
        sourceAppProps["DimensionStyleName"] != null
          ? GetDimensionStyle(sourceAppProps["DimensionStyleName"] as string)
          : Doc.Database.Dimstyle;
      if (dimensionStyle == ObjectId.Null)
        dimensionStyle = Doc.Database.Dimstyle;
      Point3d position = PointToNative(dimension.position);
      string className = sourceAppProps["class"] as string;
      switch (className)
      {
        case "AlignedDimension":
          var alignedSpeckle = dimension as DistanceDimension;
          if (alignedSpeckle == null || alignedSpeckle.measured.Count < 2)
            return null;
          try
          {
            var alignedStart = PointToNative(alignedSpeckle.measured[0]);
            var alignedEnd = PointToNative(alignedSpeckle.measured[1]);
            var alignedDimension = new AlignedDimension(
              alignedStart,
              alignedEnd,
              position,
              dimension.value,
              dimensionStyle
            );
            Utilities.SetApplicationProps(alignedDimension, typeof(AlignedDimension), sourceAppProps);
            _dimension = alignedDimension;
          }
          catch { }
          ;
          break;
        case "RotatedDimension":
          var rotatedSpeckle = dimension as DistanceDimension;
          if (rotatedSpeckle == null || rotatedSpeckle.measured.Count < 2)
            return null;
          double rotation = sourceAppProps["Rotation"] as double? ?? 0;
          try
          {
            var rotatedStart = PointToNative(rotatedSpeckle.measured[0]);
            var rotatedEnd = PointToNative(rotatedSpeckle.measured[1]);
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
          }
          catch { }
          break;
        case "OrdinateDimension":
          try
          {
            var ordinateDimension = DimensionToNative(dimension) as OrdinateDimension;
            if (ordinateDimension != null)
              Utilities.SetApplicationProps(ordinateDimension, typeof(OrdinateDimension), sourceAppProps);
            _dimension = ordinateDimension;
          }
          catch { }
          break;
        case "RadialDimension":
          try
          {
            var radialDimension = DimensionToNative(dimension) as RadialDimension;
            if (radialDimension != null)
              Utilities.SetApplicationProps(radialDimension, typeof(RadialDimension), sourceAppProps);
            _dimension = radialDimension;
          }
          catch { }
          break;
        case "DiametricDimension":
          var diametricSpeckle = dimension as LengthDimension;
          if (diametricSpeckle == null || diametricSpeckle.measured as Line == null)
            return null;
          try
          {
            var line = diametricSpeckle.measured as Line;
            var start = PointToNative(line.start);
            var end = PointToNative(line.end);
            double leaderLength = ScaleToNative(sourceAppProps["LeaderLength"] as double? ?? 0, ModelUnits);
            var diametricDimension = new DiametricDimension(end, start, leaderLength, dimension.value, dimensionStyle);
            sourceAppProps["LeaderLength"] = leaderLength;
            Utilities.SetApplicationProps(diametricDimension, typeof(DiametricDimension), sourceAppProps);
            _dimension = diametricDimension;
          }
          catch { }
          break;
        case "ArcDimension":
          var arcSpeckle = dimension as LengthDimension;
          if (arcSpeckle == null || arcSpeckle.measured as Arc == null)
            return null;
          try
          {
            var arc = ArcToNative(arcSpeckle.measured as Arc);
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
          catch { }
          break;
        case "LineAngularDimension2":
          try
          {
            var lineAngularDimension = DimensionToNative(dimension) as LineAngularDimension2;
            if (lineAngularDimension != null)
              Utilities.SetApplicationProps(lineAngularDimension, typeof(LineAngularDimension2), sourceAppProps);
            _dimension = lineAngularDimension;
          }
          catch { }
          break;
        case "Point3AngularDimension":
          try
          {
            var pointAngularDimension = DimensionToNative(dimension) as Point3AngularDimension;
            if (pointAngularDimension != null)
              Utilities.SetApplicationProps(pointAngularDimension, typeof(Point3AngularDimension), sourceAppProps);
            _dimension = pointAngularDimension;
          }
          catch { }
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
      AcadDB.Dimension _dimension = null;
      var style = Doc.Database.Dimstyle;
      var position = PointToNative(dimension.position);
      switch (dimension)
      {
        case LengthDimension o:
          switch (o.measured)
          {
            case Arc a:
              var arcCenter = PointToNative(a.plane.origin);
              var arcStart = PointToNative(a.startPoint);
              var arcEnd = PointToNative(a.endPoint);
              try
              {
                var arcDimension = new ArcDimension(arcCenter, arcStart, arcEnd, position, dimension.value, style);
                _dimension = arcDimension;
              }
              catch { }
              break;
            case Line l:
              var radialStart = PointToNative(l.start);
              var radialEnd = PointToNative(l.end);
              double leaderLength = radialEnd.DistanceTo(position);
              try
              {
                var radialDimension = new RadialDimension(radialStart, radialEnd, leaderLength, dimension.value, style);
                _dimension = radialDimension;
              }
              catch { }
              break;
          }
          break;
        case AngleDimension o:
          try
          {
            if (o.measured.Count < 2)
              break;
            var line1Start = PointToNative(o.measured[0].start);
            var line1End = PointToNative(o.measured[0].end);
            var line2Start = PointToNative(o.measured[1].start);
            var line2End = PointToNative(o.measured[1].end);
            if (Math.Round(line1Start.DistanceTo(line2Start), 3) == 0)
              _dimension = new Point3AngularDimension(line1Start, line1End, line2End, position, dimension.value, style);
            else
              _dimension = new LineAngularDimension2(
                line1Start,
                line1End,
                line2Start,
                line2End,
                position,
                dimension.value,
                style
              );
          }
          catch { }
          break;
        case DistanceDimension o:
          if (o.measured.Count < 2)
            break;
          try
          {
            var start = PointToNative(o.measured[0]);
            var end = PointToNative(o.measured[1]);
            var normal = VectorToNative(o.direction);

            if (o.isOrdinate)
            {
              bool useXAxis = normal.IsParallelTo(Vector3d.XAxis) ? true : false;
              var ordinateDimension = new OrdinateDimension(useXAxis, end, position, dimension.value, style);
              ordinateDimension.Origin = start;
              _dimension = ordinateDimension;
            }
            else
            {
              var dir = new Vector3d(end.X - start.X, end.Y - start.Y, end.Z - start.Z); // dimension direction
              var angleBetween = Math.Round(dir.GetAngleTo(normal), 3);
              if (dir.IsParallelTo(normal, Tolerance.Global))
                _dimension = new AlignedDimension(start, end, position, dimension.value, style);
              else
                _dimension = new RotatedDimension(angleBetween, start, end, position, dimension.value, style);
            }
          }
          catch { }
          break;
        default:
          break;
      }
      //if (_dimension != null)
      // _dimension.TextPosition = PointToNative(dimension.textPosition);
      return _dimension;
    }

    private ObjectId GetDimensionStyle(string styleName)
    {
      var dimStyleTable = Trans.GetObject(Doc.Database.DimStyleTableId, OpenMode.ForRead) as DimStyleTable;
      foreach (ObjectId id in dimStyleTable)
      {
        var dimStyle = Trans.GetObject(id, OpenMode.ForRead) as DimStyleTableRecord;
        if (dimStyle.Name == styleName)
          return id;
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
        _proxy[AutocadPropName] = props;

      return _proxy;
    }
  }
}
