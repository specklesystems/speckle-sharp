using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using AcadBRep = Autodesk.AutoCAD.BoundaryRepresentation;
using AcadDB = Autodesk.AutoCAD.DatabaseServices;

using BlockInstance = Objects.Other.BlockInstance;
using BlockDefinition = Objects.Other.BlockDefinition;
using Hatch = Objects.Other.Hatch;
using HatchLoop = Objects.Other.HatchLoop;
using HatchLoopType = Objects.Other.HatchLoopType;
using Point = Objects.Geometry.Point;
using Text = Objects.Other.Text;
using Speckle.Core.Models;
using Speckle.Core.Kits;
using Autodesk.AutoCAD.Windows.Data;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
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
      /*
      // skip if dynamic block
      if (reference.IsDynamicBlock)
        return null;
      */

      // get record
      BlockDefinition definition = null;
      var attributes = new Dictionary<string, string>();

      BlockTableRecord btr = (BlockTableRecord)Trans.GetObject(reference.BlockTableRecord, OpenMode.ForRead);
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
        transform = reference.BlockTransform.ToArray(),
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
      double[] transform = instance.transform;
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
        if (CanConvertToSpeckle(obj))
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
        name = record.Name,
        basePoint = PointToSpeckle(record.Origin),
        geometry = geometry,
        units = ModelUnits
      };

      return definition;
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
          if (CanConvertToNative(geo))
          {
            Entity converted = null;
            switch (geo)
            {
              case BlockInstance o:
                BlockInstanceToNativeDB(o, out BlockReference reference, false);
                converted = reference;
                break;
              default:
                converted = ConvertToNative(geo) as Entity;
                break;
            }

            if (converted == null)
              continue;
            else if (!converted.IsNewObject && !(converted is BlockReference))
              bakedGeometry.Add(converted.Id);
            else
              btr.AppendEntity(converted);
          }
        }
        blockId = blckTbl.Add(btr);
        btr.AssumeOwnershipOf(bakedGeometry); // add in baked geo
        Trans.AddNewlyCreatedDBObject(btr, true);
        blckTbl.Dispose();
      }


      return blockId;
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
  }
}
