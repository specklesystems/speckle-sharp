using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AcadBRep = Autodesk.AutoCAD.BoundaryRepresentation;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;

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
using Speckle.Core.Models;
using Speckle.Core.Kits;
using Autodesk.AutoCAD.Windows.Data;
using Objects.Other;
using Autodesk.AutoCAD.Colors;
using System.Reflection;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    // Display Style
    private static LineWeight GetLineWeight(double weight)
    {
      double hundredthMM = weight * 100;
      var weights = Enum.GetValues(typeof(LineWeight)).Cast<int>().ToList();
      int closest = weights.Aggregate((x, y) => Math.Abs(x - hundredthMM) < Math.Abs(y - hundredthMM) ? x : y);
      return (LineWeight)closest;
    }
    public Entity DisplayStyleToNative(Base styleBase, Entity entity)
    {
      var color = new System.Drawing.Color();
      if (styleBase is DisplayStyle style)
      {
        color = System.Drawing.Color.FromArgb(style.color);
        entity.LineWeight = GetLineWeight(style.lineweight);
        if (LineTypeDictionary.ContainsKey(style.linetype))
          entity.LinetypeId = LineTypeDictionary[style.linetype];
      }
      else if (styleBase is RenderMaterial material) // this is the fallback value if a rendermaterial is passed instead
      {
        color = System.Drawing.Color.FromArgb(material.diffuse);
      }
      entity.Color = Color.FromRgb(color.R, color.G, color.B);
      entity.Transparency = new Transparency(color.A);

      return entity;
    }
    public DisplayStyle DisplayStyleToSpeckle(Entity entity)
    {
      var style = new DisplayStyle();
      if (entity is null) return style;

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
    public AcadDB.Hatch HatchToNativeDB(Hatch hatch)
    {
      BlockTableRecord modelSpaceRecord = Doc.Database.GetModelSpace();

      // convert curves
      var loops = new Dictionary<AcadDB.Curve, HatchLoopTypes>();
      if (hatch.loops != null)
      {
        foreach (var loop in hatch.loops)
        {
          var converted = CurveToNativeDB(loop.Curve);
          if (converted == null)
            continue;

          var curveId = modelSpaceRecord.Append(converted);
          if (curveId.IsValid)
          {
            HatchLoopTypes type = HatchLoopTypeToNative(loop.Type);
            loops.Add(converted, type);
          }
        }
      }
      else // this is just here for backwards compatibility, before loops were introduced. Deprecate a few releases after 2.2.6
      {
        foreach (var loop in hatch.curves)
        {
          var converted = CurveToNativeDB(loop);
          if (converted == null)
            continue;

          var curveId = modelSpaceRecord.Append(converted);
          if (curveId.IsValid)
            loops.Add(converted, HatchLoopTypes.Default);
        }
      }
      if (loops.Count == 0) return null;

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
        _hatch.AppendLoop(entry.Value, new ObjectIdCollection() { entry.Key.ObjectId });
      _hatch.EvaluateHatch(true);
      foreach (var entry in loops) // delete created hatch curves
        entry.Key.Erase();

      return _hatch;
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
        btrObjId = reference.AnonymousBlockTableRecord != ObjectId.Null ? 
          reference.AnonymousBlockTableRecord : reference.DynamicBlockTableRecord;

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
        transform = new Transform( reference.BlockTransform.ToArray(), ModelUnits ),
        blockDefinition = definition,
        units = ModelUnits
      };

      // add attributes
      instance["attributes"] = attributes;

      return instance;
    }
    public string BlockInstanceToNativeDB(BlockInstance instance, out BlockReference reference, bool AppendToModelSpace = true)
    {
      string result = null;
      reference = null;

      // block definition
      ObjectId definitionId = BlockDefinitionToNativeDB(instance.blockDefinition);
      if (definitionId == ObjectId.Null)
        return result;

      // insertion pt
      Point3d insertionPoint = PointToNative(instance.GetInsertionPoint());

      // transform
      double[] transform = instance.transform.value;
      for (int i = 3; i < 12; i += 4)
        transform[i] = ScaleToNative(transform[i], instance.units);
      Matrix3d convertedTransform = new Matrix3d(transform);

      // add block reference
      BlockTableRecord modelSpaceRecord = Doc.Database.GetModelSpace();
      BlockReference br = new BlockReference(insertionPoint, definitionId);
      br.BlockTransform = convertedTransform;
      // add attributes if there are any
      var attributes = instance["attributes"] as Dictionary<string, string>;
      if (attributes != null)
      {
        // TODO: figure out how to add attributes
      }
      ObjectId id = ObjectId.Null;
      if (AppendToModelSpace)
        id = modelSpaceRecord.Append(br);

      // return
      result = "success";
      if ((id.IsValid && !id.IsNull) || !AppendToModelSpace)
        reference = br;

      return result;
    }
    public BlockDefinition BlockRecordToSpeckle (BlockTableRecord record)
    {
      // skip if this is from an external reference
      if (record.IsFromExternalReference)
        return null;

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
            converted["Layer"] = objEntity.Layer;
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
        BlockReference reference = referenceId != ObjectId.Null ? Trans.GetObject(referenceId, OpenMode.ForRead) as BlockReference : null;
        if (reference == null) return fullName;

        if (btr.IsAnonymous)
        {
          BlockTableRecord dynamicBlock = reference.DynamicBlockTableRecord != ObjectId.Null ? 
            Trans.GetObject(reference.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord : null;
          if (dynamicBlock != null) fullName = dynamicBlock.Name;
        }

        foreach (DynamicBlockReferenceProperty prop in reference.DynamicBlockReferencePropertyCollection)
          curVisibilityName = prop.Value is double o ? o.ToString() : (string)prop.Value;
        if (!string.IsNullOrEmpty(curVisibilityName)) fullName = $"{fullName}_{curVisibilityName}";
      }

      return fullName;
    }

    public ObjectId BlockDefinitionToNativeDB(BlockDefinition definition)
    {
      // get modified definition name with commit info
      var blockName = RemoveInvalidAutocadChars($"{Doc.UserData["commit"]} - {definition.name}");

      ObjectId blockId = ObjectId.Null;

      // see if block record already exists and return if so
      BlockTable blckTbl = Trans.GetObject(Doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
      if (blckTbl.Has(blockName))
        return blckTbl[blockName];

      // create btr
      using (BlockTableRecord btr = new BlockTableRecord())
      {
        btr.Name = blockName;

        // base point
        btr.Origin = PointToNative(definition.basePoint);

        // add geometry
        blckTbl.UpgradeOpen();
        var bakedGeometry = new ObjectIdCollection(); // this is to contain block def geometry that is already added to doc space during conversion
        foreach (var geo in definition.geometry)
        {
          List<Entity> converted = new List<Entity>();
          DisplayStyle style = geo["displayStyle"] as DisplayStyle;
          RenderMaterial material = geo["renderMaterial"] as RenderMaterial;
          if (CanConvertToNative(geo))
          {
            switch (geo)
            {
              case BlockInstance o:
                BlockInstanceToNativeDB(o, out BlockReference reference, false);
                if (reference != null) converted.Add(reference);
                break;
              default:
                ConvertWithDisplay(geo, style, material, ref converted);
                break;
            }
          }
          else if (geo["displayValue"] != null)
          {
            switch (geo["displayValue"])
            {
              case Base o:
                ConvertWithDisplay(o, style, material, ref converted, true);
                break;
              case IReadOnlyList<Base> o:
                foreach (Base displayValue in o)
                  ConvertWithDisplay(displayValue, style, material, ref converted, true);
                break;
            }
          }
          foreach (var convertedItem in converted)
          {
            if (!convertedItem.IsNewObject && !(convertedItem is BlockReference))
              bakedGeometry.Add(convertedItem.Id);
            else
              btr.AppendEntity(convertedItem);
          }
        }
        blockId = blckTbl.Add(btr);
        btr.AssumeOwnershipOf(bakedGeometry); // add in baked geo
        Trans.AddNewlyCreatedDBObject(btr, true);
        blckTbl.Dispose();
      }

      return blockId;
    }
    private void ConvertWithDisplay(Base obj, DisplayStyle style, RenderMaterial material, ref List<Entity> converted, bool TryUseObjDisplay = false)
    {
      if (TryUseObjDisplay && obj["displayStyle"] as DisplayStyle != null) style = obj["displayStyle"] as DisplayStyle;
      if (TryUseObjDisplay && obj["renderMaterial"] as RenderMaterial != null) material = obj["renderMaterial"] as RenderMaterial;
      var convertedGeo = ConvertToNative(obj) as Entity;
      if (convertedGeo != null)
      {
        convertedGeo = (style != null) ? DisplayStyleToNative(style, convertedGeo) : DisplayStyleToNative(material, convertedGeo);
        converted.Add(convertedGeo);
      }
    }

    // Text
    public Text TextToSpeckle(DBText text)
    {
      var _text = new Text();

      // not realistically feasible to extract outline curves for displayvalue currently
      _text.height = text.Height;
      var center = GetTextCenter(text);
      _text.plane = PlaneToSpeckle( new Plane(center, text.Normal));
      _text.rotation = text.Rotation;
      _text.value = text.TextString;
      _text.units = ModelUnits;

      // autocad specific props
      _text["horizontalAlignment"] = text.HorizontalMode.ToString();
      _text["verticalAlignment"] = text.VerticalMode.ToString();
      _text["position"] = PointToSpeckle(text.Position);
      _text["widthFactor"] = text.WidthFactor;
      _text["isMText"] = false;

      return _text;
    }
    public Text TextToSpeckle(MText text)
    {
      var _text = new Text();

      // not realistically feasible to extract outline curves for displayvalue currently
      _text.height = text.Height;
      var center = (text.Bounds != null) ? GetTextCenter(text.Bounds.Value) : text.Location;
      _text.plane = PlaneToSpeckle( new Plane(center, text.Normal));
      _text.rotation = text.Rotation;    
      _text.value = text.Contents;
      _text.richText = text.ContentsRTF;
      _text.units = ModelUnits;

      // autocad specific props
      _text["position"] = PointToSpeckle(text.Location);
      _text["isMText"] = true;

      return _text;
    }
    public MText MTextToNative(Text text)
    {
      var _text = new MText();

      if (string.IsNullOrEmpty(text.richText))
        _text.Contents = text.value;
      else
        _text.ContentsRTF = text.richText;
      _text.TextHeight = ScaleToNative(text.height, text.units);
      _text.Location = (text["position"] != null) ? PointToNative(text["position"] as Point) : PointToNative(text.plane.origin);
      _text.Rotation = text.rotation;
      _text.Normal = VectorToNative(text.plane.normal);

      return _text;
    }
    public DBText DBTextToNative(Text text)
    {
      var _text = new DBText();
      _text.TextString = text.value;
      _text.Height = ScaleToNative(text.height, text.units);
      _text.Position = (text["position"] != null) ? PointToNative(text["position"] as Point) : PointToNative(text.plane.origin);
      _text.Rotation = text.rotation;
      _text.Normal = VectorToNative(text.plane.normal);
      double widthFactor = text["widthFactor"] as double? ?? 1;
      _text.WidthFactor = widthFactor;

      return _text;
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
      double x = position.X; double y = position.Y; double z = position.Z;

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
          x = alignment.X;  y = alignment.Y + (height / 2);
          break;
        case AttachmentPoint.TopCenter:
        case AttachmentPoint.TopMid:
          x = alignment.X;  y = alignment.Y - (height / 2);
          break;
        case AttachmentPoint.MiddleRight:
          x = alignment.X - ((alignment.X - position.X) / 2); y = alignment.Y;
          break;
        case AttachmentPoint.BottomRight:
          x = alignment.X - ((alignment.X - position.X) / 2); y = alignment.Y + (height / 2);
          break;
        case AttachmentPoint.TopRight:
          x = alignment.X - ((alignment.X - position.X) / 2); y = alignment.Y - (height / 2);
          break;
        case AttachmentPoint.MiddleCenter:
        case AttachmentPoint.MiddleMid:
          x = alignment.X; y = alignment.Y;
          break;
        default:
          break;
      }
      return new Point3d(x, y, z);
    }

    // Dimension
    public Dimension DimensionToSpeckle(AcadDB.Dimension dimension)
    {
      var textPosition = PointToSpeckle(dimension.TextPosition); //this is important for accurate toNative conversions, add to autocad props

      switch (dimension)
      {
        case AlignedDimension o:
          var alignedDimension = new DistanceDimension() { units = ModelUnits, text = dimension.DimensionText, measurement = dimension.Measurement };
          var alignedNormal = new Vector3d(o.XLine2Point.X - o.XLine1Point.X, o.XLine2Point.Y - o.XLine1Point.Y, o.XLine2Point.Z - o.XLine1Point.Z).GetPerpendicularVector();
          alignedDimension.direction = VectorToSpeckle(alignedNormal);
          alignedDimension.position = PointToSpeckle(o.DimLinePoint);
          alignedDimension.measured = new List<Point>() { PointToSpeckle(o.XLine1Point), PointToSpeckle(o.XLine2Point) };
          var alignedProps = GetAutoCADProps(o, typeof(AlignedDimension), true);
          alignedProps["TextPosition"]= textPosition;
          alignedDimension[AutocadPropName] = alignedProps;
          return alignedDimension;
        case RotatedDimension o:
          var rotatedDimension = new DistanceDimension() { units = ModelUnits, text = dimension.DimensionText, measurement = dimension.Measurement };
          var rotatedNormal = new Vector3d(o.XLine2Point.X - o.XLine1Point.X, o.XLine2Point.Y - o.XLine1Point.Y, o.XLine2Point.Z - o.XLine1Point.Z).GetPerpendicularVector();
          rotatedDimension.direction = VectorToSpeckle(rotatedNormal.RotateBy(o.Rotation, Vector3d.ZAxis));
          rotatedDimension.position = PointToSpeckle(o.DimLinePoint);
          rotatedDimension.measured = new List<Point>() { PointToSpeckle(o.XLine1Point), PointToSpeckle(o.XLine2Point) };
          var rotatedProps = GetAutoCADProps(o, typeof(RotatedDimension), true);
          rotatedProps["TextPosition"] = textPosition;
          rotatedDimension[AutocadPropName] = rotatedProps;
          return rotatedDimension;
        case OrdinateDimension o:
          var ordinateDimension = new DistanceDimension() { units = ModelUnits, text = dimension.DimensionText, measurement = dimension.Measurement };
          ordinateDimension.direction = o.UsingXAxis ? VectorToSpeckle(Vector3d.XAxis) : VectorToSpeckle(Vector3d.YAxis);
          ordinateDimension.position = PointToSpeckle(o.LeaderEndPoint);
          ordinateDimension.measured = new List<Point>() { PointToSpeckle(o.Origin), PointToSpeckle(o.DefiningPoint) };
          var ordinateProps = GetAutoCADProps(o, typeof(OrdinateDimension), true);
          ordinateProps["TextPosition"] = textPosition;
          ordinateDimension[AutocadPropName] = ordinateProps;
          return ordinateDimension;
        case RadialDimension o:
          var radialDimension = new LengthDimension() { units = ModelUnits, text = dimension.DimensionText, measurement = dimension.Measurement};
          radialDimension.measured = LineToSpeckle(new Line3d(o.Center, o.ChordPoint));
          radialDimension.position = PointToSpeckle(o.ChordPoint); // TODO: the position could be improved by using the leader length x the direction of the dimension
          var radialProps = GetAutoCADProps(o, typeof(RadialDimension), true);
          radialProps["TextPosition"] = textPosition;
          radialDimension[AutocadPropName] = radialProps;
          return radialDimension;
        case DiametricDimension o:
          var diametricDimension = new LengthDimension() { units = ModelUnits, text = dimension.DimensionText, measurement = dimension.Measurement };
          diametricDimension.measured = LineToSpeckle(new Line3d(o.FarChordPoint, o.ChordPoint));
          diametricDimension.position = PointToSpeckle(o.ChordPoint); // TODO: the position could be improved by using the leader length x the direction of the dimension
          var diametricProps = GetAutoCADProps(o, typeof(DiametricDimension), true);
          diametricProps["TextPosition"] = textPosition;
          diametricDimension[AutocadPropName] = diametricProps;
          return diametricDimension;
        case ArcDimension o:
          var arcDimension = new LengthDimension() { units = ModelUnits, text = dimension.DimensionText, measurement = dimension.Measurement };
          arcDimension.measured = ArcToSpeckle(new CircularArc3d(o.XLine1Point, o.ArcPoint, o.XLine2Point));
          arcDimension.position = PointToSpeckle(o.ArcPoint);
          var arcProps = GetAutoCADProps(o, typeof(ArcDimension), true);
          arcProps["TextPosition"] = textPosition;
          arcDimension[AutocadPropName] = arcProps;
          return arcDimension;
        case LineAngularDimension2 o:
          var lineAngularDimension = new AngleDimension() { units = ModelUnits, text = dimension.DimensionText, measurement = dimension.Measurement };
          var line1 = new Line3d(o.XLine1Start, o.XLine1End);
          var line2 = new Line3d(o.XLine2Start, o.XLine2End);
          lineAngularDimension.measured = new List<Line>() { LineToSpeckle(line1), LineToSpeckle(line2) };
          lineAngularDimension.position = PointToSpeckle(o.ArcPoint);
          var lineAngularProps = GetAutoCADProps(o, typeof(LineAngularDimension2), true);
          lineAngularProps["TextPosition"] = textPosition;
          lineAngularDimension[AutocadPropName] = lineAngularProps;
          return lineAngularDimension;
        case Point3AngularDimension o:
          var pointAngularDimension = new AngleDimension() { units = ModelUnits, text = dimension.DimensionText, measurement = dimension.Measurement };
          var point1 = new Line3d(o.ArcPoint, o.XLine1Point);
          var point2 = new Line3d(o.ArcPoint, o.XLine2Point);
          pointAngularDimension.measured = new List<Line>() { LineToSpeckle(point1), LineToSpeckle(point2) };
          pointAngularDimension.position = PointToSpeckle(o.ArcPoint);
          var pointAngularProps = GetAutoCADProps(o, typeof(Point3AngularDimension), true);
          pointAngularProps["TextPosition"] = textPosition;
          pointAngularDimension[AutocadPropName] = pointAngularProps;
          return pointAngularDimension;
        default:
          return null;
      }
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
    public AcadDB.Dimension AcadDimensionToNative(Dimension dimension)
    {
      AcadDB.Dimension _dimension = null;
      Base sourceAppProps = dimension[AutocadPropName] as Base;
      string dimensionText = sourceAppProps != null && sourceAppProps["DimensionText"] != null ? sourceAppProps["DimensionText"] as string : dimension.text;
      Point textPosition = sourceAppProps != null && sourceAppProps["TextPosition"] != null ? sourceAppProps["TextPosition"] as Point : dimension.position;
      ObjectId dimensionStyle = sourceAppProps != null && sourceAppProps["DimensionStyleName"] != null ? GetDimensionStyle(sourceAppProps["DimensionStyleName"] as string) : Doc.Database.Dimstyle;
      if (string.IsNullOrEmpty(dimensionText)) dimensionText = dimension.measurement.ToString();
      if (dimensionStyle == ObjectId.Null) dimensionStyle = Doc.Database.Dimstyle;
      Point3d position = PointToNative(dimension.position);
      string className = sourceAppProps != null ? sourceAppProps["class"] as string : string.Empty;
      switch (className)
      {
        case "AlignedDimension":
          var alignedSpeckle = dimension as DistanceDimension;
          if (alignedSpeckle == null || alignedSpeckle.measured.Count < 2) break;
          try
          {
            var alignedStart = PointToNative(alignedSpeckle.measured[0]);
            var alignedEnd = PointToNative(alignedSpeckle.measured[1]);
            var alignedDimension = new AlignedDimension(alignedStart, alignedEnd, position, dimensionText, dimensionStyle);
            SetAutoCADProps(alignedDimension, typeof(AlignedDimension), sourceAppProps);
            _dimension = alignedDimension;
          }
          catch { };
          break;
        case "RotatedDimension":
          var rotatedSpeckle = dimension as DistanceDimension;
          if (rotatedSpeckle == null || rotatedSpeckle.measured.Count < 2) break;
          double? rotation = sourceAppProps["Rotation"] != null ? sourceAppProps["Rotation"] as double? : 0;
          try
          {
            var rotatedStart = PointToNative(rotatedSpeckle.measured[0]);
            var rotatedEnd = PointToNative(rotatedSpeckle.measured[1]);
            var rotatedDimension = new RotatedDimension((double)rotation, rotatedStart, rotatedEnd, position, dimensionText, dimensionStyle);
            SetAutoCADProps(rotatedDimension, typeof(RotatedDimension), sourceAppProps);
            _dimension = rotatedDimension;
          }
          catch { }
          break;
        case "OrdinateDimension":
          var ordinateSpeckle = dimension as DistanceDimension;
          if (ordinateSpeckle == null || ordinateSpeckle.measured.Count < 2 || ordinateSpeckle.direction == null) break;
          bool useXAxis = VectorToNative(ordinateSpeckle.direction).IsParallelTo(Vector3d.YAxis) ? false : true;
          try
          {
            var ordinateDefining = PointToNative(ordinateSpeckle.measured[1]);
            var ordinateDimension = new OrdinateDimension(useXAxis, ordinateDefining, position, dimensionText, dimensionStyle);
            SetAutoCADProps(ordinateDimension, typeof(OrdinateDimension), sourceAppProps);
            _dimension = ordinateDimension;
          }
          catch { }
          break;
        case "RadialDimension":
          var radialSpeckle = dimension as LengthDimension;
          if (radialSpeckle == null || radialSpeckle.measured as Line == null) break;
          try
          {
            var radialLine = LineToNative(radialSpeckle.measured as Line);
            double leaderLength = radialLine.EndPoint.DistanceTo(position);
            var radialDimension = new RadialDimension(radialLine.StartPoint, radialLine.EndPoint, leaderLength, dimensionText, dimensionStyle);
            SetAutoCADProps(radialDimension, typeof(RadialDimension), sourceAppProps);
            _dimension = radialDimension;
          }
          catch { }
          break;
        case "DiametricDimension":
          var diametricSpeckle = dimension as LengthDimension;
          if (diametricSpeckle == null || diametricSpeckle.measured as Line == null) break;
          try
          {
            var diametricLine = LineToNative(diametricSpeckle.measured as Line);
            double leaderLength = diametricLine.EndPoint.DistanceTo(position);
            var diametricDimension = new DiametricDimension(diametricLine.EndPoint, diametricLine.StartPoint, leaderLength, dimensionText, dimensionStyle);
            SetAutoCADProps(diametricDimension, typeof(DiametricDimension), sourceAppProps);
            _dimension = diametricDimension;
          }
          catch { }
          break;
        case "ArcDimension":
          var arcSpeckle = dimension as LengthDimension;
          if (arcSpeckle == null || arcSpeckle.measured as Arc == null) break;
          try
          {
            var arc = ArcToNative(arcSpeckle.measured as Arc);
            var arcDimension = new ArcDimension(arc.Center, arc.StartPoint, arc.EndPoint, position, dimensionText, dimensionStyle);
            SetAutoCADProps(arcDimension, typeof(ArcDimension), sourceAppProps);
            _dimension = arcDimension;
          }
          catch { }
          break;
        case "LineAngularDimension2":
          var lineAngularSpeckle = dimension as AngleDimension;
          if (lineAngularSpeckle == null || lineAngularSpeckle.measured.Count < 2) break;
          try
          {
            var lineStart = LineToNative(lineAngularSpeckle.measured[0]);
            var lineEnd = LineToNative(lineAngularSpeckle.measured[1]);
            var lineAngularDimension = new LineAngularDimension2(lineStart.StartPoint, lineStart.EndPoint, lineEnd.StartPoint, lineEnd.EndPoint, position, dimensionText, dimensionStyle);
            SetAutoCADProps(lineAngularDimension, typeof(LineAngularDimension2), sourceAppProps);
            _dimension = lineAngularDimension;
          }
          catch { }
          break;
        case "Point3AngularDimension":
          var pointAngularSpeckle = dimension as AngleDimension;
          if (pointAngularSpeckle == null || pointAngularSpeckle.measured.Count < 2) break;
          try
          {
            var lineStart = LineToNative(pointAngularSpeckle.measured[0]);
            var lineEnd = LineToNative(pointAngularSpeckle.measured[1]);
            var pointAngularDimension = new Point3AngularDimension(lineStart.StartPoint, lineStart.EndPoint, lineEnd.EndPoint, position, dimensionText, dimensionStyle);
            SetAutoCADProps(pointAngularDimension, typeof(Point3AngularDimension), sourceAppProps);
            _dimension = pointAngularDimension;
          }
          catch { }
          break;
        default:
          _dimension = DimensionToNative(dimension);
          break;
      }
      _dimension.TextPosition = PointToNative(textPosition);
      return _dimension;
    }
    public AcadDB.Dimension DimensionToNative(Dimension dimension)
    {
      AcadDB.Dimension _dimension = null;
      var position = PointToNative(dimension.position);
      string textValue = dimension.text;
      switch (dimension)
      {
        case LengthDimension o:
          switch (o.measured)
          {
            case Arc a:
              var arc = ArcToNative(a);
              try
              {
                var arcDimension = new ArcDimension(arc.Center, arc.StartPoint, arc.EndPoint, position, textValue, Doc.Database.Dimstyle);
                _dimension = arcDimension;
              }
              catch { }
              break;
            case Line l:
              var radialLine = LineToNative(l);
              double leaderLength = radialLine.EndPoint.DistanceTo(position);
              try
              {
                var radialDimension = new RadialDimension(radialLine.StartPoint, radialLine.EndPoint, leaderLength, textValue, Doc.Database.Dimstyle);
                _dimension = radialDimension;
              }
              catch { }
              break;
          }
          break;
        case AngleDimension o:
          if (o.measured.Count < 2) break;
          var lineStart = LineToNative(o.measured[0] as Line);
          var lineEnd = LineToNative(o.measured[1] as Line);
          try
          {
            var lineAngularDimension = new LineAngularDimension2(lineStart.StartPoint, lineStart.EndPoint, lineEnd.StartPoint, lineEnd.EndPoint, position, textValue, Doc.Database.Dimstyle);
            _dimension = lineAngularDimension;
          }
          catch { }
          break;
        case DistanceDimension o:
          if (o.measured.Count < 2) break;
          // see if the dimension direction is perpendicular to the line between the two measured points
          var start = PointToNative(o.measured[0]);
          var end = PointToNative(o.measured[1]);
          var normal = VectorToNative(o.direction);
          var dir = new Vector3d(end.X - start.X, end.Y - start.Y, end.Z - start.Z);
          try
          {
            if (normal.IsPerpendicularTo(dir))
            {
              var alignedDimension = new AlignedDimension(start, end, position, textValue, Doc.Database.Dimstyle);
              _dimension = alignedDimension;
            }
            else
            {
              var angle = dir.GetPerpendicularVector().GetAngleTo(normal);
              var rotatedDimension = new RotatedDimension(angle, start, end, position, textValue, Doc.Database.Dimstyle);
              _dimension = rotatedDimension;
            }
          }
          catch { }
          break;
        default:
          break;
      }
      return _dimension;
    }
  }
}
