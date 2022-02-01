using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using BE = Objects.BuiltElements;
using BER = Objects.BuiltElements.Revit;
using BERC = Objects.BuiltElements.Revit.Curve;
using DB = Autodesk.Revit.DB;
using STR = Objects.Structural;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit : ISpeckleConverter
  {
#if REVIT2023
    public static string RevitAppName = Applications.Revit2023;
#elif REVIT2022
    public static string RevitAppName = Applications.Revit2022;
#elif REVIT2021
    public static string RevitAppName = Applications.Revit2021;
#elif REVIT2020
    public static string RevitAppName = Applications.Revit2020;
#else
    public static string RevitAppName = Applications.Revit2019;
#endif

    #region ISpeckleConverter props

    public string Description => "Default Speckle Kit for Revit";
    public string Name => nameof(ConverterRevit);
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";

    public IEnumerable<string> GetServicedApplications() => new string[] { RevitAppName };

    #endregion ISpeckleConverter props

    public Document Doc { get; private set; }

    /// <summary>
    /// <para>To know which other objects are being converted, in order to sort relationships between them.
    /// For example, elements that have children use this to determine whether they should send their children out or not.</para>
    /// </summary>
    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    /// <summary>
    /// <para>To keep track of previously received objects from a given stream in here. If possible, conversions routines
    /// will edit an existing object, otherwise they will delete the old one and create the new one.</para>
    /// </summary>
    public List<ApplicationPlaceholderObject> PreviousContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    /// <summary>
    /// Keeps track of the current host element that is creating any sub-objects it may have.
    /// </summary>
    public HostObject CurrentHostElement { get; set; }

    /// <summary>
    /// Used when sending; keeps track of all the converted objects so far. Child elements first check in here if they should convert themselves again (they may have been converted as part of a parent's hosted elements).
    /// </summary>
    public List<string> ConvertedObjectsList { get; set; } = new List<string>();

    public ProgressReport Report { get; private set; } = new ProgressReport();

    public Dictionary<string, BE.Level> Levels { get; private set; } = new Dictionary<string, BE.Level>();

    public ConverterRevit()
    {
      var ver = System.Reflection.Assembly.GetAssembly(typeof(ConverterRevit)).GetName().Version;
      Report.Log($"Using converter: {this.Name} v{ver}");
    }

    public void SetContextDocument(object doc)
    {
      Doc = (Document)doc;
      Report.Log($"Using document: {Doc.PathName}");
      Report.Log($"Using units: {ModelUnits}");
    }

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;
    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects) => PreviousContextObjects = objects;
    public void SetConverterSettings(object settings)
    {
      throw new NotImplementedException("This converter does not have any settings.");
    }

    public Base ConvertToSpeckle(object @object)
    {
      Base returnObject = null;
      switch (@object)
      {
        case DB.DetailCurve o:
          returnObject = DetailCurveToSpeckle(o);
          Report.Log($"Converted DetailCurve {o.Id}");
          break;
        case DB.DirectShape o:
          returnObject = DirectShapeToSpeckle(o);
          Report.Log($"Converted DirectShape {o.Id}");
          break;
        case DB.FamilyInstance o:
          returnObject = FamilyInstanceToSpeckle(o);
          Report.Log($"Converted FamilyInstance {o.Id}");
          break;
        case DB.Floor o:
          returnObject = FloorToSpeckle(o);
          Report.Log($"Converted Floor {o.Id}");
          break;
        case DB.Level o:
          returnObject = LevelToSpeckle(o);
          Report.Log($"Converted Level {o.Id}");
          break;
        case DB.View o:
          returnObject = ViewToSpeckle(o);
          Report.Log($"Converted View {o.ViewType} {o.Id}");
          break;
        case DB.ModelCurve o:

          if ((BuiltInCategory)o.Category.Id.IntegerValue == BuiltInCategory.OST_RoomSeparationLines)
          {
            returnObject = RoomBoundaryLineToSpeckle(o);
          }
          else if ((BuiltInCategory)o.Category.Id.IntegerValue == BuiltInCategory.OST_MEPSpaceSeparationLines)
          {
            returnObject = SpaceSeparationLineToSpeckle(o);
          }
          else
          {
            returnObject = ModelCurveToSpeckle(o);
          }
          Report.Log($"Converted ModelCurve {o.Id}");
          break;
        case DB.Opening o:
          returnObject = OpeningToSpeckle(o);
          Report.Log($"Converted Opening {o.Id}");
          break;
        case DB.RoofBase o:
          returnObject = RoofToSpeckle(o);
          Report.Log($"Converted RoofBase {o.Id}");
          break;
        case DB.Area o:
          returnObject = AreaToSpeckle(o);
          Report.Log($"Converted Area {o.Id}");
          break;
        case DB.Architecture.Room o:
          returnObject = RoomToSpeckle(o);
          Report.Log($"Converted Room {o.Id}");
          break;
        case DB.Architecture.TopographySurface o:
          returnObject = TopographyToSpeckle(o);
          Report.Log($"Converted Topography {o.Id}");
          break;
        case DB.Wall o:
          returnObject = WallToSpeckle(o);
          Report.Log($"Converted Wall {o.Id}");
          break;
        case DB.Mechanical.Duct o:
          returnObject = DuctToSpeckle(o);
          Report.Log($"Converted Duct {o.Id}");
          break;
        case DB.Mechanical.FlexDuct o:
          returnObject = DuctToSpeckle(o);
          Report.Log($"Converted FlexDuct {o.Id}");
          break;
        case DB.Mechanical.Space o:
          returnObject = SpaceToSpeckle(o);
          Report.Log($"Converted Space {o.Id}");
          break;
        case DB.Plumbing.Pipe o:
          returnObject = PipeToSpeckle(o);
          Report.Log($"Converted Pipe {o.Id}");
          break;
        case DB.Plumbing.FlexPipe o:
          returnObject = PipeToSpeckle(o);
          Report.Log($"Converted FlexPipe {o.Id}");
          break;
        case DB.Electrical.Wire o:
          returnObject = WireToSpeckle(o);
          Report.Log($"Converted Wire {o.Id}");
          break;
        //these should be handled by curtain walls
        case DB.CurtainGridLine _:
          returnObject = null;
          break;
        case DB.Architecture.BuildingPad o:
          returnObject = BuildingPadToSpeckle(o);
          Report.Log($"Converted BuildingPad {o.Id}");
          break;
        case DB.Architecture.Stairs o:
          returnObject = StairToSpeckle(o);
          Report.Log($"Converted Stairs {o.Id}");
          break;
        //these are handled by Stairs
        case DB.Architecture.StairsRun _:
          returnObject = null;
          break;
        case DB.Architecture.StairsLanding _:
          returnObject = null;
          break;
        case DB.Architecture.Railing o:
          returnObject = RailingToSpeckle(o);
          Report.Log($"Converted Railing {o.Id}");
          break;
        case DB.Architecture.TopRail _:
          returnObject = null;
          break;
        case DB.Structure.Rebar o:
          returnObject = RebarToSpeckle(o);
          Report.Log($"Converted Rebar {o.Id}");
          break;
        case DB.Ceiling o:
          returnObject = CeilingToSpeckle(o);
          Report.Log($"Converted Ceiling {o.Id}");
          break;
        case DB.PointCloudInstance o:
          returnObject = PointcloudToSpeckle(o);
          Report.Log($"Converted PointCloudInstance {o.Id}");
          break;
        case DB.ProjectInfo o:
          returnObject = ProjectInfoToSpeckle(o);
          Report.Log($"Converted ProjectInfo");
          break;
        case DB.ElementType o:
          returnObject = ElementTypeToSpeckle(o);
          Report.Log($"Converted ElementType {o.Id}");
          break;
        case DB.Grid o:
          returnObject = GridLineToSpeckle(o);
          Report.Log($"Converted Grid {o.Id}");
          break;
        case DB.ReferencePoint o:
          if ((BuiltInCategory)o.Category.Id.IntegerValue == BuiltInCategory.OST_AnalyticalNodes)
          {
            returnObject = AnalyticalNodeToSpeckle(o);
            Report.Log($"Converted AnalyticalNode {o.Id}");
          }
          break;
        case DB.Structure.BoundaryConditions o:
          returnObject = BoundaryConditionsToSpeckle(o);
          Report.Log($"Converted BoundaryConditions {o.Id}");
          break;
        case DB.Structure.AnalyticalModelStick o:
          returnObject = AnalyticalStickToSpeckle(o);
          Report.Log($"Converted AnalyticalStick {o.Id}");
          break;
        case DB.Structure.AnalyticalModelSurface o:
          returnObject = AnalyticalSurfaceToSpeckle(o);
          Report.Log($"Converted AnalyticalSurface {o.Id}");
          break;
        default:
          // if we don't have a direct conversion, still try to send this element as a generic RevitElement
          var el = @object as Element;
          if (el.IsElementSupported())
          {
            returnObject = RevitElementToSpeckle(el);
            Report.Log($"Converted {el.Category.Name} {el.Id}");
            break;
          }

          Report.Log($"Skipped not supported type: {@object.GetType()}{GetElemInfo(@object)}");
          returnObject = null;
          break;
      }

      // NOTE: Only try generic method assignment if there is no existing render material from conversions;
      // we might want to try later on to capture it more intelligently from inside conversion routines.
      if (returnObject != null && returnObject["renderMaterial"] == null)
      {
        var material = GetElementRenderMaterial(@object as DB.Element);
        returnObject["renderMaterial"] = material;
      }

      return returnObject;
    }

    private string GetElemInfo(object o)
    {
      if (o is Element e)
      {
        return $", name: {e.Name}, id: {e.UniqueId}";
      }

      return "";
    }

    public object ConvertToNative(Base @object)
    {
      //Family Document
      if (Doc.IsFamilyDocument)
      {
        switch (@object)
        {
          case ICurve o:
            return ModelCurveToNative(o);
          case Geometry.Brep o:
            return FreeformElementToNativeFamily(o);
          case Geometry.Mesh o:
            return FreeformElementToNativeFamily(o);
          default:
            return null;
        }
      }

      //Project Document
      // schema check
      var speckleSchema = @object["@SpeckleSchema"] as Base;
      if (speckleSchema != null)
      {
        // find self referential prop and set value to @object if it is null (happens when sent from gh)
        if (CanConvertToNative(speckleSchema))
        {
          var prop = speckleSchema.GetInstanceMembers().Where(o => speckleSchema[o.Name] == null)?.Where(o => o.PropertyType.IsAssignableFrom(@object.GetType()))?.FirstOrDefault();
          if (prop != null)
            speckleSchema[prop.Name] = @object;
          @object = speckleSchema;
        }
      }

      switch (@object)
      {
        //geometry
        case ICurve o:
          Report.Log($"Created ModelCurve");
          return ModelCurveToNative(o);

        case Geometry.Brep o:
          Report.Log($"Created Brep {o.applicationId}");
          return DirectShapeToNative(o);

        case Geometry.Mesh o:
          Report.Log($"Created Mesh {o.applicationId}");
          return DirectShapeToNative(o);

        // non revit built elems
        case BE.Alignment o:
          if (o.curves is null) // TODO: remove after a few releases, this is for backwards compatibility
          {
            Report.Log($"Created Alignment {o.applicationId}");
            return ModelCurveToNative(o.baseCurve);
          }
          Report.Log($"Created Alignment {o.applicationId} as Curves");
          return AlignmentToNative(o);

        case BE.Structure o:
          Report.Log($"Created Structure {o.applicationId}");
          return DirectShapeToNative(o.displayMesh);

        //built elems
        case BER.AdaptiveComponent o:
          Report.Log($"Created AdaptiveComponent {o.applicationId}");
          return AdaptiveComponentToNative(o);

        case BE.Beam o:
          Report.Log($"Created Beam {o.applicationId}");
          return BeamToNative(o);

        case BE.Brace o:
          Report.Log($"Created Brace {o.applicationId}");
          return BraceToNative(o);

        case BE.Column o:
          Report.Log($"Created Column {o.applicationId}");
          return ColumnToNative(o);

#if REVIT2022
        case BE.Ceiling o:
          return CeilingToNative(o);
#endif

        case BERC.DetailCurve o:
          Report.Log($"Created DetailCurve {o.applicationId}");
          return DetailCurveToNative(o);

        case BER.DirectShape o:
          Report.Log($"Created DirectShape {o.applicationId}");
          return DirectShapeToNative(o);

        case BER.FreeformElement o:
          Report.Log($"Created FreeFormElement {o.applicationId}");
          return FreeformElementToNative(o);

        case BER.FamilyInstance o:
          Report.Log($"Created FamilyInstance {o.applicationId}");
          return FamilyInstanceToNative(o);

        case BE.Floor o:
          Report.Log($"Created Floor {o.applicationId}");
          return FloorToNative(o);

        case BE.Level o:
          Report.Log($"Created Level {o.applicationId}");
          return LevelToNative(o);

        case BERC.ModelCurve o:
          Report.Log($"Created ModelCurve {o.applicationId}");
          return ModelCurveToNative(o);

        case BE.Opening o:
          Report.Log($"Created Opening {o.applicationId}");
          return OpeningToNative(o);

        case BERC.RoomBoundaryLine o:
          Report.Log($"Created RoomBoundaryLine {o.applicationId}");
          return RoomBoundaryLineToNative(o);

        case BERC.SpaceSeparationLine o:
          Report.Log($"Created Brep {o.applicationId}");
          return SpaceSeparationLineToNative(o);

        case BE.Roof o:
          Report.Log($"Created Roof {o.applicationId}");
          return RoofToNative(o);

        case BE.Topography o:
          Report.Log($"Created Topography {o.applicationId}");
          return TopographyToNative(o);

        case BER.RevitProfileWall o:
          Report.Log($"Created RevitProfileWall {o.applicationId}");
          return ProfileWallToNative(o);

        case BER.RevitFaceWall o:
          Report.Log($"Created RevitFaceWall {o.applicationId}");
          return FaceWallToNative(o);

        case BE.Wall o:
          Report.Log($"Created Wall {o.applicationId}");
          return WallToNative(o);

        case BE.Duct o:
          Report.Log($"Created Duct {o.applicationId}");
          return DuctToNative(o);

        case BE.Pipe o:
          Report.Log($"Created Pipe {o.applicationId}");
          return PipeToNative(o);

        case BE.Wire o:
          Report.Log($"Created Wire {o.applicationId}");
          return WireToNative(o);

        case BE.Revit.RevitRailing o:
          Report.Log($"Created RevitRailing {o.applicationId}");
          return RailingToNative(o);

        case BER.ParameterUpdater o:
          UpdateParameter(o);
          return null;

        case BE.View3D o:
          Report.Log($"Created View3D {o.applicationId}");
          return ViewToNative(o);

        case BE.Room o:
          Report.Log($"Created Room {o.applicationId}");
          return RoomToNative(o);

        case BE.GridLine o:
          Report.Log($"Created Gridline {o.applicationId}");
          return GridLineToNative(o);

        case BE.Space o:
          Report.Log($"Created Space {o.applicationId}");
          return SpaceToNative(o);
        //Structural 
        case STR.Geometry.Element1D o:
          Report.Log($"Created Element1D {o.applicationId}");
          return AnalyticalStickToNative(o);

        case STR.Geometry.Element2D o:
          Report.Log($"Created Element2D {o.applicationId}");
          return AnalyticalSurfaceToNative(o);

        case STR.Geometry.Node o:
          Report.Log($"Created Node {o.applicationId}");
          return AnalyticalNodeToNative(o);


        case STR.Analysis.Model o:
          Report.Log($"Created StructuralModel");
          return StructuralModelToNative(o);

        // other
        case Other.BlockInstance o:
          Report.Log($"Created BlockInstance {o.applicationId}");
          return BlockInstanceToNative(o);

        default:
          return null;
      }
    }

    public List<Base> ConvertToSpeckle(List<object> objects) => objects.Select(ConvertToSpeckle).ToList();

    public List<object> ConvertToNative(List<Base> objects) => objects.Select(ConvertToNative).ToList();

    public bool CanConvertToSpeckle(object @object)
    {
      return @object
      switch
      {
        DB.DetailCurve _ => true,
        DB.DirectShape _ => true,
        DB.FamilyInstance _ => true,
        DB.Floor _ => true,
        DB.Level _ => true,
        DB.View _ => true,
        DB.ModelCurve _ => true,
        DB.Opening _ => true,
        DB.RoofBase _ => true,
        DB.Area _ => true,
        DB.Architecture.Room _ => true,
        DB.Architecture.TopographySurface _ => true,
        DB.Wall _ => true,
        DB.Mechanical.Duct _ => true,
        DB.Mechanical.FlexDuct _ => true,
        DB.Mechanical.Space _ => true,
        DB.Plumbing.Pipe _ => true,
        DB.Plumbing.FlexPipe _ => true,
        DB.Electrical.Wire _ => true,
        DB.CurtainGridLine _ => true, //these should be handled by curtain walls
        DB.Architecture.BuildingPad _ => true,
        DB.Architecture.Stairs _ => true,
        DB.Architecture.StairsRun _ => true,
        DB.Architecture.StairsLanding _ => true,
        DB.Architecture.Railing _ => true,
        DB.Architecture.TopRail _ => true,
        DB.Ceiling _ => true,
        DB.PointCloudInstance _ => true,
        DB.Group _ => true,
        DB.ProjectInfo _ => true,
        DB.ElementType _ => true,
        DB.Grid _ => true,
        DB.ReferencePoint _ => true,
        DB.Structure.AnalyticalModelStick _ => true,
        DB.Structure.AnalyticalModelSurface _ => true,
        DB.Structure.BoundaryConditions _ => true,
        _ => (@object as Element).IsElementSupported()
      };
    }

    public bool CanConvertToNative(Base @object)
    {
      //Family Document
      if (Doc.IsFamilyDocument)
      {
        return @object
        switch
        {
          ICurve _ => true,
          Geometry.Brep _ => true,
          Geometry.Mesh _ => true,
          _ => false
        };
      }


      //Project Document
      var schema = @object["@SpeckleSchema"] as Base; // check for contained schema
      if (schema != null)
        return CanConvertToNative(schema);

      return @object
      switch
      {
        //geometry
        ICurve _ => true,
        Geometry.Brep _ => true,
        Geometry.Mesh _ => true,
        // non revit built elems
        BE.Structure _ => true,
        BE.Alignment _ => true,
        //built elems
        BER.AdaptiveComponent _ => true,
        BE.Beam _ => true,
        BE.Brace _ => true,
        BE.Column _ => true,
#if REVIT2022
        BE.Ceiling _ => true,
#endif
        BERC.DetailCurve _ => true,
        BER.DirectShape _ => true,
        BER.FreeformElement _ => true,
        BER.FamilyInstance _ => true,
        BE.Floor _ => true,
        BE.Level _ => true,
        BERC.ModelCurve _ => true,
        BE.Opening _ => true,
        BERC.RoomBoundaryLine _ => true,
        BERC.SpaceSeparationLine _ => true,
        BE.Roof _ => true,
        BE.Topography _ => true,
        BER.RevitFaceWall _ => true,
        BER.RevitProfileWall _ => true,
        BE.Wall _ => true,
        BE.Duct _ => true,
        BE.Pipe _ => true,
        BE.Wire _ => true,
        BE.Revit.RevitRailing _ => true,
        BER.ParameterUpdater _ => true,
        BE.View3D _ => true,
        BE.Room _ => true,
        BE.GridLine _ => true,
        BE.Space _ => true,
        //Structural
        STR.Geometry.Element1D _ => true,
        STR.Geometry.Element2D _ => true,
        STR.Geometry.Node _ => true,
        STR.Analysis.Model _ => true,
        Other.BlockInstance _ => true,
        _ => false

      };
    }
  }
}
