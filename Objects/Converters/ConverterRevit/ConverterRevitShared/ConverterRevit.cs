using Autodesk.Revit.DB;
using BE = Objects.BuiltElements;
using BER = Objects.BuiltElements.Revit;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    public Element CurrentHostElement { get; set; }

    public List<string> ConvertedObjectsList { get; set; } = new List<string>();

    public HashSet<Error> ConversionErrors { get; private set; } = new HashSet<Error>();

    public Dictionary<string, BE.Level> Levels { get; private set; } = new Dictionary<string, BE.Level>();

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
        case BER.AdaptiveComponent o:
          return AdaptiveComponentToNative(o);

        case BE.Beam o:
          return BeamToNative(o);

        case BE.Brace o:
          return BraceToNative(o);

        case BE.Column o:
          return ColumnToNative(o);

        case BER.DetailCurve o:
          return DetailCurveToNative(o);

        case BER.DirectShape o:
          return DirectShapeToNative(o);

        case BER.FamilyInstance o:
          return FamilyInstanceToNative(o);

        case BE.Floor o:
          return FloorToNative(o);

        case BE.Level o:
          return LevelToNative(o);

        case BER.ModelCurve o:
          return ModelCurveToNative(o);

        case BE.Opening o:
          return OpeningToNative(o);

        case BER.RoomBoundaryLine o:
          return RoomBoundaryLineToNative(o);

        case BE.Roof o:
          return RoofToNative(o);

        case BE.Topography o:
          return TopographyToNative(o);

        case BE.Wall o:
          return WallToNative(o);

        case BE.Duct o:
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
        case BER.AdaptiveComponent _:
          return true;

        case BE.Beam _:
          return true;

        case BE.Brace _:
          return true;

        case BE.Column _:
          return true;

        case BER.DetailCurve _:
          return true;

        case BER.DirectShape _:
          return true;

        case BER.FamilyInstance _:
          return true;

        case BE.Floor _:
          return true;

        case BE.Level _:
          return true;

        case BER.ModelCurve _:
          return true;

        case BE.Opening _:
          return true;

        case BER.RoomBoundaryLine _:
          return true;

        case BE.Roof _:
          return true;

        case BE.Topography _:
          return true;

        case BE.Wall _:
          return true;

        case BE.Duct _:
          return true;

        default:
          return false;
      }
    }
  }
}