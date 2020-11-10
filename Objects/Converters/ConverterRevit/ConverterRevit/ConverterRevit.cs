using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.Revit;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

using DB = Autodesk.Revit.DB;

using DetailCurve = Objects.Revit.DetailCurve;
using DirectShape = Objects.Revit.DirectShape;
using RevitFamilyInstance = Objects.Revit.RevitFamilyInstance;
using Floor = Objects.BuiltElements.Floor;
using Level = Objects.BuiltElements.Level;
using ModelCurve = Objects.Revit.ModelCurve;
using Opening = Objects.BuiltElements.Opening;
using Wall = Objects.BuiltElements.Wall;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit : ISpeckleConverter
  {
    #region implemented props

    public string Description => "Default Speckle Kit for Revit";
    public string Name => nameof(ConverterRevit);
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";

    public IEnumerable<string> GetServicedApplications() => new string[] { Applications.Revit };

    #endregion implemented props

    public ConverterRevit()
    {
    }

    private double Scale { get; set; } = 3.2808399;

    public Document Doc { get; private set; }

    public HashSet<Error> ConversionErrors { get; private set; } = new HashSet<Error>();

    public void SetContextDocument(object doc)
    {
      Doc = (Autodesk.Revit.DB.Document)doc;
    }

    public Base ConvertToSpeckle(object @object)
    {
      switch (@object)
      {
        case DB.DetailCurve o:
          return DetailCurveToSpeckle(o) as Base;

        case DB.DirectShape o:
          return DirectShapeToSpeckle(o) as Base;

        case DB.FamilyInstance o:
          return FamilyInstanceToSpeckle(o) as Base;

        case DB.Floor o:
          return FloorToSpeckle(o) as Base;

        case DB.Level o:
          return LevelToSpeckle(o);

        case DB.ModelCurve o:
          if ((BuiltInCategory)o.Category.Id.IntegerValue == BuiltInCategory.OST_RoomSeparationLines)
            return RoomBoundaryLineToSpeckle(o);
          return ModelCurveToSpeckle(o);

        case DB.Opening o:
          return OpeningToSpeckle(o);

        case DB.RoofBase o:
          return RoofToSpeckle(o);

        case DB.Architecture.Room o:
          return RoomToSpeckle(o);

        case DB.Architecture.TopographySurface o:
          return TopographyToSpeckle(o);

        case DB.Wall o:
          return WallToSpeckle(o) as Base;

        case DB.Mechanical.Duct o:
          return DuctToSpeckle(o);

        default:
          ConversionErrors.Add(new Error("Type not supported", $"Cannot convert {@object.GetType()} to Speckle"));
          return null;
      }
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      var converted = objects.Select(x => ConvertToSpeckle(x)).ToList();
      return NestHstedObjects(converted, objects.Select(x => x as DB.Element).ToList());
    }

    public object ConvertToNative(Base @object)
    {
      switch (@object)
      {
        case AdaptiveComponent o:
          return AdaptiveComponentToNative(o);

        case Beam o:
          return BeamToNative(o);

        case Brace o:
          return BraceToNative(o);

        case Column o:
          return ColumnToNative(o);

        case DetailCurve o:
          return DetailCurveToNative(o);

        case DirectShape o:
          return DirectShapeToNative(o);

        case RevitFamilyInstance o:
          return FamilyInstanceToNative(o);

        case Floor o:
          return FloorToNative(o);

        case Level o:
          return LevelToNative(o);

        case ModelCurve o:
          return ModelCurveToNative(o);

        case Opening o:
          return OpeningToNative(o);

        case RoomBoundaryLine o:
          return RoomBoundaryLineToNative(o);

        case Roof o:
          return RoofToNative(o);

        case Topography o:
          return TopographyToNative(o);

        case Wall o:
          return WallToNative(o);

        case Duct o:
          return DuctToNative(o);

        default:
          ConversionErrors.Add(new Error("Type not supported", $"Cannot convert {@object.GetType()} to Revit"));
          return null;
      }
    }

    /// <summary>
    /// Converts a list of speckle objects to Revit, assumes the objects have already been nested
    /// TODO: make sure nesting happens in Grasshopper/Rhino too
    /// </summary>
    /// <param name="objects"></param>
    /// <returns></returns>
    public List<object> ConvertToNative(List<Base> objects)
    {
      var converted = new List<object>();
      foreach (var obj in objects)
      {
        var c = ConvertToNative(obj) as DB.Element;
        converted.Add(c);
        //process nested elements afterwards
        var nested = obj.GetMemberSafe("@hostedElements", new List<Base>());
        converted.AddRange(ConvertBatchToNativeWithHost(nested, c.Id.IntegerValue));
      }

      return converted;
    }

    private List<object> ConvertBatchToNativeWithHost(List<Base> objects, int hostId)
    {
      var converted = new List<object>();
      foreach (var obj in objects)
      {
        if (hostId != -1)
          obj["revitHostId"] = hostId;
        var c = ConvertToNative(obj) as DB.Element;
        converted.Add(c);
        //process nested elements afterwards
        var nested = obj.GetMemberSafe("@hostedElements", new List<Base>());
        converted.AddRange(ConvertBatchToNativeWithHost(nested, c.Id.IntegerValue));
      }

      return converted;
    }

    private List<Base> NestHstedObjects(List<Base> baseObjs, List<DB.Element> revitObjs)
    {
      Dictionary<int, Base> nested = new Dictionary<int, Base>();
      if (baseObjs.Count != revitObjs.Count)
        throw new Exception("Object count must be equal");

      for (var i = 0; i < baseObjs.Count; i++)
      {
        var revitObj = revitObjs[i];
        var baseObj = baseObjs[i];

        if (baseObj == null //conversion failed
          || (revitObj as DB.FamilyInstance == null && revitObj as DB.Opening == null) // not a family instance nor opening
          || (revitObj is DB.FamilyInstance && (revitObj as DB.FamilyInstance).Host == null) // family instance not hosted
          || (revitObj is DB.FamilyInstance && (revitObj as DB.FamilyInstance).HostFace != null) // family instance face hosted
          || (revitObj is DB.Opening && (revitObj as DB.Opening).Host == null)) // opening not hosted
        {
          nested.Add(revitObj.Id.IntegerValue, baseObj);
          continue;
        }

        var hostIndex = -1;

        if (revitObj is DB.FamilyInstance)
          hostIndex = revitObjs.FindIndex(x => x.Id == (revitObj as DB.FamilyInstance).Host.Id);
        else if (revitObj is DB.Opening)
          hostIndex = revitObjs.FindIndex(x => x.Id == (revitObj as DB.Opening).Host.Id);

        if (hostIndex == -1) //host not in current selection
        {
          nested.Add(revitObj.Id.IntegerValue, baseObj);
          continue;
        }

        var hostElem = revitObjs[hostIndex];
        //if already in the nested list, add child to it, otherwise to the baseObject
        if (nested.ContainsKey(hostElem.Id.IntegerValue))
        {
          nested[hostElem.Id.IntegerValue].GetMemberSafe("@hostedElements", new List<Base>()).Add(baseObj);
        }
        else
        {
          baseObjs[hostIndex].GetMemberSafe("@hostedElements", new List<Base>()).Add(baseObj);
        }
      }
      return nested.Select(x => x.Value).ToList();
    }

    //private MethodInfo ConversionMethods(object @object, string methodName)
    //{
    //  var methods = typeof(Conversion).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
    //    .Where(m => m.Name == methodName);
    //  var par = methods.ElementAt(1).GetParameters()[0].ParameterType;
    //  var par2 = @object.GetType();
    //  //is there any method that takes in the above type as input?
    //  return typeof(Conversion).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)
    //    .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Any(p => p.ParameterType == @object.GetType()));
    //}

    public bool CanConvertToSpeckle(object @object)
    {
      throw new NotImplementedException();
    }

    public bool CanConvertToNative(Base @object)
    {
      throw new NotImplementedException();
    }
  }
}