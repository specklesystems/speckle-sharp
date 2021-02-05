using Autodesk.Revit.DB;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System.Collections.Generic;
using System.Linq;
using BE = Objects.BuiltElements;
using BER = Objects.BuiltElements.Revit;
using BERC = Objects.BuiltElements.Revit.Curve;
using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit : ISpeckleConverter
  {
#if REVIT2021
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

    public HashSet<Error> ConversionErrors { get; private set; } = new HashSet<Error>();

    public Dictionary<string, BE.Level> Levels { get; private set; } = new Dictionary<string, BE.Level>();

    public ConverterRevit() { }

    public void SetContextDocument(object doc) => Doc = (Document)doc;

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;
    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects) => PreviousContextObjects = objects;

    public Base ConvertToSpeckle(object @object)
    {
      Base returnObject = null;
      switch (@object)
      {
        case DB.DetailCurve o:
          returnObject = DetailCurveToSpeckle(o);
          break;
        case DB.DirectShape o:
          returnObject = DirectShapeToSpeckle(o);
          break;
        case DB.FamilyInstance o:
          returnObject = FamilyInstanceToSpeckle(o);
          break;
        case DB.Floor o:
          returnObject = FloorToSpeckle(o);
          break;
        case DB.Level o:
          returnObject = LevelToSpeckle(o);
          break;
        case DB.ModelCurve o:

          if ((BuiltInCategory)o.Category.Id.IntegerValue == BuiltInCategory.OST_RoomSeparationLines)
          {
            returnObject = RoomBoundaryLineToSpeckle(o);
          }
          else
          {
            returnObject = ModelCurveToSpeckle(o);
          }
          break;
        case DB.Opening o:
          returnObject = OpeningToSpeckle(o);
          break;
        case DB.RoofBase o:
          returnObject = RoofToSpeckle(o);
          break;
        case DB.Architecture.Room o:
          returnObject = RoomToSpeckle(o);
          break;
        case DB.Architecture.TopographySurface o:
          returnObject = TopographyToSpeckle(o);
          break;
        case DB.Wall o:
          returnObject = WallToSpeckle(o);
          break;
        case DB.Mechanical.Duct o:
          returnObject = DuctToSpeckle(o);
          break;
        //these should be handled by curtain walls
        case DB.CurtainGridLine _:
          returnObject = null;
          break;
        case DB.Architecture.BuildingPad o:
          returnObject = BuildingPadToSpeckle(o);
          break;
        case DB.Architecture.Stairs o:
          returnObject = StairToSpeckle(o);
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
          break;
        case DB.Architecture.TopRail _:
          returnObject = null;
          break;
        case DB.Ceiling o:
          returnObject = CeilingToSpeckle(o);
          break;
        case DB.ProjectInfo o:
          returnObject = ProjectInfoToSpeckle(o);
          break;
        case DB.ElementType o:
          returnObject = ElementTypeToSpeckle(o);
          break;
        default:
          ConversionErrors.Add(new Error("Type not supported", $"Cannot convert {@object.GetType()} to Speckle"));
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

    public object ConvertToNative(Base @object)
    {
      switch (@object)
      {
        case BER.AdaptiveComponent o:
          return AdaptiveComponentToNative(o);

        case BE.Beam o:
          return BeamToNative(o);

        case BE.Brace o:
          return BraceToNative(o);

        case BE.Column o:
          return ColumnToNative(o);

        case BERC.DetailCurve o:
          return DetailCurveToNative(o);

        case BER.DirectShape o:
          return DirectShapeToNative(o);

        case BER.FamilyInstance o:
          return FamilyInstanceToNative(o);

        case BE.Floor o:
          return FloorToNative(o);

        case BE.Level o:
          return LevelToNative(o);

        case BERC.ModelCurve o:
          return ModelCurveToNative(o);

        case BE.Opening o:
          return OpeningToNative(o);

        case BERC.RoomBoundaryLine o:
          return RoomBoundaryLineToNative(o);

        case BE.Roof o:
          return RoofToNative(o);

        case BE.Topography o:
          return TopographyToNative(o);

        case BER.RevitFaceWall o:
          return FaceWallToNative(o);

        case BE.Wall o:
          return WallToNative(o);

        case BE.Duct o:
          return DuctToNative(o);

        case BE.Revit.RevitRailing o:
          return RailingToNative(o);

        default:
          return null;
      }
    }

    public List<Base> ConvertToSpeckle(List<object> objects) => objects.Select(o => ConvertToSpeckle(o)).ToList();

    public List<object> ConvertToNative(List<Base> objects) => objects.Select(o => ConvertToNative(o)).ToList();

    public bool CanConvertToSpeckle(object @object)
    {
      switch (@object)
      {
        case DB.DetailCurve _:
          return true;

        case DB.DirectShape _:
          return true;

        case DB.FamilyInstance _:
          return true;

        case DB.Floor _:
          return true;

        case DB.Level _:
          return true;

        case DB.ModelCurve _:
          return true;

        case DB.Opening _:
          return true;

        case DB.RoofBase _:
          return true;

        case DB.Architecture.Room _:
          return true;

        case DB.Architecture.TopographySurface _:
          return true;

        case DB.Wall _:
          return true;

        case DB.Mechanical.Duct _:
          return true;

        //these should be handled by curtain walls
        case DB.CurtainGridLine _:
          return true;

        case DB.Architecture.BuildingPad _:
          return true;

        case DB.Architecture.Stairs _:
          return true;

        case DB.Architecture.StairsRun _:
          return true;

        case DB.Architecture.StairsLanding _:
          return true;

        case DB.Architecture.Railing _:
          return true;

        case DB.Architecture.TopRail _:
          return true;

        case DB.Ceiling _:
          return true;

        case DB.Group _:
          return true;

        case DB.ProjectInfo _:
          return true;

        case DB.ElementType _:
          return true;

        default:
          return false;
      }
    }

    public bool CanConvertToNative(Base @object)
    {
      switch (@object)
      {
        case BER.AdaptiveComponent _:
          return true;

        case BE.Beam _:
          return true;

        case BE.Brace _:
          return true;

        case BE.Column _:
          return true;

        case BERC.DetailCurve _:
          return true;

        case BER.DirectShape _:
          return true;

        case BER.FamilyInstance _:
          return true;

        case BE.Floor _:
          return true;

        case BE.Level _:
          return true;

        case BERC.ModelCurve _:
          return true;

        case BE.Opening _:
          return true;

        case BERC.RoomBoundaryLine _:
          return true;

        case BE.Roof _:
          return true;

        case BE.Topography _:
          return true;

        case BER.RevitFaceWall _:
          return true;

        case BE.Wall _:
          return true;

        case BE.Duct _:
          return true;

        case BE.Revit.RevitRailing _:
          return true;

        default:
          return false;
      }
    }
  }
}