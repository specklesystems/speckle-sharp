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
    #region ISpeckleConverter props

    public string Description => "Default Speckle Kit for Revit";
    public string Name => nameof(ConverterRevit);
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";

    public IEnumerable<string> GetServicedApplications() => new string[] { Applications.Revit };

    #endregion ISpeckleConverter props

    public ConverterRevit()
    {
    }

    public Document Doc { get; private set; }

    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;

    public HashSet<Error> ConversionErrors { get; private set; } = new HashSet<Error>();

    public Dictionary<string, RevitLevel> Levels { get; private set; } = new Dictionary<string, RevitLevel>();

    public void SetContextDocument(object doc)
    {
      Doc = (Document)doc;
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

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      var elements = objects.Select(x => x as DB.Element).ToList();
      var converted = objects.Select(x => ConvertToSpeckle(x)).ToList();
      var hostObjects = NestHostedObjects(converted, elements);
      var levelWithObjects = NestObjectsInLevels(hostObjects);
      return levelWithObjects;
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
      var levels = objects.Where(x => x is ILevel);
      var nonLevels = objects.Where(x => !(x is ILevel));

      var sortedObjects = new List<Base>();
      sortedObjects.AddRange(levels); // add the levels first
      sortedObjects.AddRange(levels.Cast<ILevel>().SelectMany(x => x.elements)); // add their sub elements
      sortedObjects.AddRange(nonLevels); // add everything else

      var converted = new List<object>();
      foreach (var obj in sortedObjects)
      {
        try
        {
          var conversionResult = ConvertToNative(obj);
          var revitElement = conversionResult as DB.Element;
          if (revitElement == null)
            continue;
          converted.Add(revitElement);

          //process nested elements afterwards
          //this will take care of levels and host elements
          if (obj["@elements"] != null && obj["@elements"] is List<Base> nestedElements)
            converted.AddRange(ConvertNestedObjectsToNative(nestedElements, revitElement));
        }
        catch (Exception e)
        {
          ConversionErrors.Add(new Error("Conversion failed", e.Message));
        }
      }

      return converted;
    }

    private List<object> ConvertNestedObjectsToNative(List<Base> objects, DB.Element host)
    {
      var converted = new List<object>();
      foreach (var obj in objects)
      {
        //add level name on object, this overrides potential existing values
        if (host is DB.Level && obj is RevitElement re)
          re.level = host.Name;
        //if hosted element, use the revitHostId prop
        else if (host.Id.IntegerValue != -1 && obj is IHostable io)
          io.revitHostId = host.Id.IntegerValue;

        var conversionResult = ConvertToNative(obj);
        var revitElement = conversionResult as DB.Element;
        if (revitElement == null)
          continue;
        converted.Add(revitElement);
        //continue un-nesting
        if (obj["@elements"] != null && obj["@elements"] is List<Base> nestedElements)
          converted.AddRange(ConvertNestedObjectsToNative(nestedElements, revitElement));
      }

      return converted;
    }

    private List<Base> NestObjectsInLevels(List<Base> baseObjs)
    {
      var levelWithObjects = new List<Base>();
      foreach (var obj in baseObjs)
      {
        if (obj is RevitElement re && !string.IsNullOrEmpty(re.level))
        {
          Levels[re.level].elements.Add(re);
        }
        else
          levelWithObjects.Add(obj);
      }
      levelWithObjects.AddRange(Levels.Values);
      return levelWithObjects;
    }

    private List<Base> NestHostedObjects(List<Base> baseObjs, List<DB.Element> revitObjs)
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
          nested[hostElem.Id.IntegerValue].GetMemberSafe("@elements", new List<Base>()).Add(baseObj);
        }
        else
        {
          baseObjs[hostIndex].GetMemberSafe("@elements", new List<Base>()).Add(baseObj);
        }
      }
      return nested.Select(x => x.Value).ToList();
    }

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