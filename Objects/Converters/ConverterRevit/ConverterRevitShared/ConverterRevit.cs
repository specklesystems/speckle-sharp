using Autodesk.Revit.DB;
using Objects.Revit;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using DetailCurve = Objects.Revit.DetailCurve;
using DirectShape = Objects.Revit.DirectShape;
using ModelCurve = Objects.Revit.ModelCurve;
using RevitFamilyInstance = Objects.Revit.RevitFamilyInstance;

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

    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    public List<string> ConvertedObjectsList { get; set; } = new List<string>();

    public HashSet<Error> ConversionErrors { get; private set; } = new HashSet<Error>();

    public Dictionary<string, RevitLevel> Levels { get; private set; } = new Dictionary<string, RevitLevel>();

    public ConverterRevit() { }

    public void SetContextDocument(object doc) => Doc = (Document)doc;

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;

    public Base ConvertToSpeckle(object @object)
    {
      switch (@object)
      {
        case DB.DetailCurve o:
          return DetailCurveToSpeckle(o);

        case DB.DirectShape o:
          return DirectShapeToSpeckle(o);

        case DB.FamilyInstance o:
          return FamilyInstanceToSpeckle(o);

        case DB.Floor o:
          return FloorToSpeckle(o);

        case DB.Level o:
          return LevelToSpeckle(o);

        case DB.ModelCurve o:
          if ((BuiltInCategory)o.Category.Id.IntegerValue == BuiltInCategory.OST_RoomSeparationLines)
          {
            return RoomBoundaryLineToSpeckle(o);
          }

          return ModelCurveToSpeckle(o);

        case DB.Opening o:
          return OpeningToSpeckle(o) as Base;

        case DB.RoofBase o:
          return RoofToSpeckle(o) as Base;

        case DB.Architecture.Room o:
          return RoomToSpeckle(o);

        case DB.Architecture.TopographySurface o:
          return TopographyToSpeckle(o);

        case DB.Wall o:
          return WallToSpeckle(o) as Base;

        case DB.Mechanical.Duct o:
          return DuctToSpeckle(o) as Base;

        default:
          ConversionErrors.Add(new Error("Type not supported", $"Cannot convert {@object.GetType()} to Speckle"));
          return null;
      }
    }

    public object ConvertToNative(Base @object)
    {
      switch (@object)
      {
        case AdaptiveComponent o:
          return AdaptiveComponentToNative(o);

        case IBeam o:
          return BeamToNative(o);

        case IBrace o:
          return BraceToNative(o);

        case IColumn o:
          return ColumnToNative(o);

        case DetailCurve o:
          return DetailCurveToNative(o);

        case DirectShape o:
          return DirectShapeToNative(o);

        case RevitFamilyInstance o:
          return FamilyInstanceToNative(o);

        case IFloor o:
          return FloorToNative(o);

        case ILevel o:
          return LevelToNative(o);

        case ModelCurve o:
          return ModelCurveToNative(o);

        case IOpening o:
          return OpeningToNative(o);

        case RoomBoundaryLine o:
          return RoomBoundaryLineToNative(o);

        case IRoof o:
          return RoofToNative(o);

        case ITopography o:
          return TopographyToNative(o);

        case IWall o:
          return WallToNative(o);

        case IDuct o:
          return DuctToNative(o);

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

        default:
          return false;
      }
    }

    public bool CanConvertToNative(Base @object)
    {
      switch (@object)
      {
        case AdaptiveComponent _:
          return true;

        case IBeam _:
          return true;

        case IBrace _:
          return true;

        case IColumn _:
          return true;

        case DetailCurve _:
          return true;

        case DirectShape _:
          return true;

        case RevitFamilyInstance _:
          return true;

        case IFloor _:
          return true;

        case ILevel _:
          return true;

        case ModelCurve _:
          return true;

        case IOpening _:
          return true;

        case RoomBoundaryLine _:
          return true;

        case IRoof _:
          return true;

        case ITopography _:
          return true;

        case IWall _:
          return true;

        case IDuct _:
          return true;

        default:
          return false;
      }
    }
  }
}