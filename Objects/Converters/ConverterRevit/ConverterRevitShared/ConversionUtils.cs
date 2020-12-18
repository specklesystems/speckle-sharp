using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Floor = Objects.BuiltElements.Floor;
using Level = Objects.BuiltElements.Level;
using Parameter = Objects.BuiltElements.Revit.Parameter;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    #region hosted elements



    private bool ShouldConvertHostedElement(DB.Element element, DB.Element host)
    {
      #region host handling
      //doesn't have a host, go ahead and convert
      if (host == null)
        return true;

      // has been converted before (from a parent host), skip it
      if (ConvertedObjectsList.IndexOf(element.UniqueId) != -1)
      {
        return false;
      }

      // the parent is in our selection list,skip it, as this element will be converted by the host element
      if (ContextObjects.FindIndex(obj => obj.applicationId == host.UniqueId) != -1)
      {
        return false;
      }
      return true;

      #endregion
    }
    /// <summary>
    /// Gets the hosted element of a host and adds the to a Base object
    /// </summary>
    /// <param name="host"></param>
    /// <param name="base"></param>
    public void GetHostedElements(Base @base, HostObject host)
    {
      var hostedElementIds = host.FindInserts(true, true, true, true);
      var convertedHostedElements = new List<Base>();

      if (hostedElementIds != null)
      {
        var elementIndex = ContextObjects.FindIndex(obj => obj.applicationId == host.UniqueId);
        if (elementIndex != -1)
        {
          ContextObjects.RemoveAt(elementIndex);
        }

        foreach (var elemId in hostedElementIds)
        {
          var element = Doc.GetElement(elemId);
          var isSelectedInContextObjects = ContextObjects.FindIndex(x => x.applicationId == element.UniqueId);

          if (isSelectedInContextObjects == -1)
          {
            continue;
          }

          ContextObjects.RemoveAt(isSelectedInContextObjects);

          if (CanConvertToSpeckle(element))
          {
            var obj = ConvertToSpeckle(element);

            if (obj != null)
            {
              convertedHostedElements.Add(obj);
              ConvertedObjectsList.Add(obj.applicationId);
            }
          }
        }

        if (convertedHostedElements.Any())
        {
          @base["elements"] = convertedHostedElements;
        }
      }
    }

    public List<ApplicationPlaceholderObject> SetHostedElements(Base @base, HostObject host)
    {
      var placeholders = new List<ApplicationPlaceholderObject>();
      if (@base["elements"] != null && @base["elements"] is List<Base> elements)
      {
        CurrentHostElement = host;

        foreach (var obj in elements)
        {
          if (obj == null)
          {
            continue;
          }

          if (!CanConvertToSpeckle(obj))
          {
            ConversionErrors.Add(new Error { message = $"Skipping {obj.speckle_type}, not supported" });
            continue;
          }

          try
          {
            var res = ConvertToNative(obj);
            if (res is ApplicationPlaceholderObject apl)
            {
              placeholders.Add(apl);
            }
            else if (res is List<ApplicationPlaceholderObject> apls)
            {
              placeholders.AddRange(apls);
            }
          }
          catch
          {
            ConversionErrors.Add(new Error { message = $"Failed to create hosted element {obj.speckle_type} in {@base.applicationId}." });
          }
        }

        CurrentHostElement = null; // unset the current host element.
      }
      return placeholders;
    }

    #endregion


    #region parameters

    #region ToSpeckle
    /// <summary>
    /// Adds Instance and Type parameters, ElementId, ApplicationId and Units.
    /// </summary>
    /// <param name="speckleElement"></param>
    /// <param name="revitElement"></param>
    /// <param name="exclusions">List of BuiltInParameters or GUIDs used to indicate what parameters NOT to get,
    /// we exclude all params already defined on the top level object to avoid duplication and 
    /// potential conflicts when setting them back on the element</param>
    private void GetRevitParameters(Base speckleElement, DB.Element revitElement, List<string> exclusions = null)
    {
      var parms = GetInstanceParams(revitElement, exclusions);
      if (parms != null)
      {
        speckleElement["parameters"] = parms;
      }

      var typeParams = GetTypeParams(revitElement);
      if (typeParams != null && !(speckleElement is Level)) //ignore type props of levels..!
      {
        if ((List<Parameter>)speckleElement["parameters"] == null)
          speckleElement["parameters"] = new List<Parameter>();
        ((List<Parameter>)speckleElement["parameters"]).AddRange(typeParams);

      }

      speckleElement["elementId"] = revitElement.Id.ToString();
      speckleElement.applicationId = revitElement.UniqueId;
      speckleElement.units = ModelUnits;
    }

    //private List<string> alltimeExclusions = new List<string> { 
    //  "ELEM_CATEGORY_PARAM" };
    private List<Parameter> GetInstanceParams(DB.Element element, List<string> exclusions)
    {
      return GetParams(element, false, exclusions);
    }
    private List<Parameter> GetTypeParams(DB.Element element)
    {
      var elementType = Doc.GetElement(element.GetTypeId());

      if (elementType == null || elementType.Parameters == null)
      {
        return new List<Parameter>();
      }
      return GetParams(elementType, true);

    }

    private List<Parameter> GetParams(DB.Element element, bool isTypeParameter = false, List<string> exclusions = null)
    {
      exclusions = (exclusions != null) ? exclusions : new List<string>();

      //exclude parameters that don't have a value and those pointing to other elements as we don't support them
      var revitParameters = element.Parameters.Cast<DB.Parameter>()
        .Where(x => x.HasValue && x.StorageType != StorageType.ElementId && !exclusions.Contains(GetParamInternalName(x))).ToList();

      //exclude parameters that failed to convert
      var speckleParameters = revitParameters.Select(x => ParameterToSpeckle(x, isTypeParameter))
        .Where(x => x != null);
      return speckleParameters.OrderBy(x => x.name).ToList();
    }

    private T GetParamValue<T>(DB.Element elem, BuiltInParameter bip)
    {
      var rp = elem.get_Parameter(bip);

      if (rp == null || !rp.HasValue)
        return default;

      return (T)ParameterToSpeckle(rp).value;

    }


    //rp must HaveValue
    private Parameter ParameterToSpeckle(DB.Parameter rp, bool isTypeParameter = false)
    {
      var sp = new Parameter
      {
        name = rp.Definition.Name,
        applicationId = GetParamInternalName(rp),
        isShared = rp.IsShared,
        isReadOnly = rp.IsReadOnly,
        isTypeParameter = isTypeParameter,
        revitUnitType = rp.Definition.UnitType.ToString() //eg UT_Length
      };

      switch (rp.StorageType)
      {
        case StorageType.Double:
          // NOTE: do not use p.AsDouble() as direct input for unit utils conversion, it doesn't work.  ¯\_(ツ)_/¯
          var val = rp.AsDouble();
          try
          {
            sp.revitUnit = rp.DisplayUnitType.ToString(); //eg DUT_MILLIMITERS, this can throw!
            sp.value = UnitUtils.ConvertFromInternalUnits(val, rp.DisplayUnitType);
          }
          catch
          {
            sp.value = val;
          }
          break;
        case StorageType.Integer:
          switch (rp.Definition.ParameterType)
          {
            case ParameterType.YesNo:
              sp.value = Convert.ToBoolean(rp.AsInteger());
              break;
            default:
              sp.value = rp.AsInteger();
              break;
          }
          break;
        case StorageType.String:
          sp.value = rp.AsString();
          if (sp.value == null)
            sp.value = rp.AsValueString();
          break;
        //case StorageType.ElementId:
        //  // NOTE: if this collects too much garbage, maybe we can ignore it
        //  var id = rp.AsElementId();
        //  var e = Doc.GetElement(id);
        //  if (e != null && CanConvertToSpeckle(e))
        //    sp.value = ConvertToSpeckle(e);
        //  break;
        default:
          return null;
          break;
      }
      return sp;
    }

    #endregion

    /// <summary>
    /// </summary>
    /// <param name="revitElement"></param>
    /// <param name="speckleElement"></param>
    public void SetInstanceParameters(Element revitElement, Base speckleElement)
    {
      if (revitElement == null)
        return;


      var speckleParameters = speckleElement["parameters"] as List<Parameter>;
      if (speckleParameters == null || !speckleParameters.Any())
        return;


      // NOTE: we are using the ParametersMap here and not Parameters, as it's a much smaller list of stuff and 
      // Parameters most likely contains extra (garbage) stuff that we don't need to set anyways
      // so it's a much faster conversion. If we find that's not the case, we might need to change it in the future
      var revitParameters = revitElement.ParametersMap.Cast<DB.Parameter>().Where(x => x != null && !x.IsReadOnly);

      // Here we are creating two  dictionaries for faster lookup
      // one uses the BuiltInName / GUID the other the name as Key
      // we need both to support parameter set by Schema Builder, that might be generated with one or the other
      var revitParameterById = revitParameters.ToDictionary(x => GetParamInternalName(x), x => x);
      var revitParameterByName = revitParameters.ToDictionary(x => x.Definition.Name, x => x);

      //only loop params we can set and that actually exist on the revit element
      var filteredSpeckleParameters = speckleParameters.Where(x => !x.isReadOnly &&
      (revitParameterById.ContainsKey(x.applicationId) || revitParameterByName.ContainsKey(x.name)));

      foreach (var sp in filteredSpeckleParameters)
      {
        var rp = revitParameterById.ContainsKey(sp.applicationId) ? revitParameterById[sp.applicationId] : revitParameterByName[sp.name];
        try
        {
          switch (rp.StorageType)
          {
            case StorageType.Double:
              if (!string.IsNullOrEmpty(sp.revitUnit))
              {
                Enum.TryParse(sp.revitUnit, out DisplayUnitType sourceUnit);
                var val = UnitUtils.ConvertToInternalUnits(Convert.ToDouble(sp.value), sourceUnit);
                rp.Set(val);
              }
              else
                rp.Set(Convert.ToDouble(sp.value));
              break;

            case StorageType.Integer:
              rp.Set(Convert.ToInt32(sp.value));
              break;

            case StorageType.String:
              rp.Set(Convert.ToString(sp.value));
              break;
            default:
              break;
          }
        }
        catch (Exception ex)
        {
          continue;
        }
      }

    }

    private string GetParamInternalName(DB.Parameter rp)
    {
      //Shared parameters use a GUID to be uniquely identified
      //Other parameters use a BuiltInParameter enum
      if (rp.IsShared)
        return rp.GUID.ToString();
      else
        return (rp.Definition as InternalDefinition).BuiltInParameter.ToString();
    }

    private void TrySetParam(DB.Element elem, BuiltInParameter bip, DB.Element value)
    {
      var param = elem.get_Parameter(bip);
      if (param != null && value != null && !param.IsReadOnly)
      {
        param.Set(value.Id);
      }
    }

    private void TrySetParam(DB.Element elem, BuiltInParameter bip, double value, string units = "")
    {
      var param = elem.get_Parameter(bip);
      if (param != null && !param.IsReadOnly)
      {
        //for angles, we use the default conversion (degrees > radians)
        if (string.IsNullOrEmpty(units))
        {
          param.Set(ScaleToNative(value, param.DisplayUnitType));
        }
        else
        {
          param.Set(ScaleToNative(value, units));
        }

      }
    }



    #endregion

    #region  element types

    private T GetElementType<T>(string family, string type)
    {
      List<ElementType> types = new FilteredElementCollector(Doc).WhereElementIsElementType().OfClass(typeof(T)).ToElements().Cast<ElementType>().ToList();

      //match family and type
      var match = types.FirstOrDefault(x => x.FamilyName == family && x.Name == type);
      if (match != null)
      {
        if (match is FamilySymbol fs && !fs.IsActive)
        {
          fs.Activate();
        }

        return (T)(object)match;
      }

      //match family
      match = types.FirstOrDefault(x => x.FamilyName == family);
      if (match != null)
      {
        ConversionErrors.Add(new Error($"Missing type: {family} {type}", $"Type was replace with: {match.FamilyName} - {match.Name}"));
        if (match != null)
        {
          if (match is FamilySymbol fs && !fs.IsActive)
          {
            fs.Activate();
          }

          return (T)(object)match;
        }
      }

      // get whatever we found, could be a different category!
      if (types.Any())
      {
        match = types.FirstOrDefault();
        ConversionErrors.Add(new Error($"Missing family and type", $"The following family and type were used: {match.FamilyName} - {match.Name}"));
        if (match != null)
        {
          if (match is FamilySymbol fs && !fs.IsActive)
          {
            fs.Activate();
          }

          return (T)(object)match;
        }
      }

      throw new Exception($"Could not find any family symbol to use.");
    }

    private T GetElementType<T>(Base element)
    {
      List<ElementType> types = new List<ElementType>();
      ElementFilter filter = GetCategoryFilter(element);


      if (filter != null)
      {
        types = new FilteredElementCollector(Doc).WhereElementIsElementType().OfClass(typeof(T)).WherePasses(filter).ToElements().Cast<ElementType>().ToList();
      }
      else
      {
        types = new FilteredElementCollector(Doc).WhereElementIsElementType().OfClass(typeof(T)).ToElements().Cast<ElementType>().ToList();
      }

      if (types.Count == 0)
      {
        throw new Exception($"Could not find any type symbol to use for family {nameof(T)}.");
      }

      var family = element["family"] as string;
      var type = element["type"] as string;

      ElementType match = null;

      //if (family == null && type == null)
      //{
      //  match = types.First();
      //}

      if (family != null && type != null)
      {
        match = types.FirstOrDefault(x => x.FamilyName == family && x.Name == type);
      }

      //some elements only have one family so we didn't add such prop our schema
      if (match == null && family == null && type != null)
      {
        match = types.FirstOrDefault(x => x.Name == type);
      }

      if (match == null && family != null) // try and match the family only.
      {
        match = types.FirstOrDefault(x => x.FamilyName == family);
        if (match != null) //inform user that the type is different!
          ConversionErrors.Add(new Error($"Missing type. Family: {family} Type: {type}", $"Type was replaced with: {match.FamilyName}, {match.Name}"));

      }

      if (match == null) // okay, try something!
      {
        match = types.First();
        ConversionErrors.Add(new Error($"Missing type. Family: {family} Type: {type}", $"Type was replaced with: {match.FamilyName}, {match.Name}"));
      }

      if (match is FamilySymbol fs && !fs.IsActive)
      {
        fs.Activate();
      }

      return (T)(object)match;
    }


    private ElementFilter GetCategoryFilter(Base element)
    {
      ElementFilter filter = null;
      if (element is BuiltElements.Wall)
      {
        filter = new ElementMulticategoryFilter(Categories.wallCategories);
      }
      else if (element is Column)
      {
        filter = new ElementMulticategoryFilter(Categories.columnCategories);
      }
      else if (element is Beam || element is Brace)
      {
        filter = new ElementMulticategoryFilter(Categories.beamCategories);
      }
      else if (element is Duct)
      {
        filter = new ElementMulticategoryFilter(Categories.ductCategories);
      }
      else if (element is Floor)
      {
        filter = new ElementMulticategoryFilter(Categories.floorCategories);
      }
      else if (element is Roof)
      {
        filter = new ElementCategoryFilter(BuiltInCategory.OST_Roofs);
      }
      else
      {
        //try get category from the parameters
        if (element["parameters"] != null && element["parameters"] is Dictionary<string, object> dic && dic.ContainsKey("Category"))
        {
          var cat = Doc.Settings.Categories.Cast<Category>().FirstOrDefault(x => x.Name == dic["Category"].ToString());
          if (cat != null)
            filter = new ElementMulticategoryFilter(new List<ElementId> { cat.Id });
        }
      }
      return filter;
    }

    #endregion

    #region conversion "edit existing if possible" utilities

    /// <summary>
    /// Returns, if found, the corresponding doc element and its corresponding local state object.
    /// The doc object can be null if the user deleted it. 
    /// </summary>
    /// <param name="applicationId"></param>
    /// <returns></returns>
    public DB.Element GetExistingElementByApplicationId(string applicationId)
    {
      var @ref = ContextObjects.FirstOrDefault(o => o.applicationId == applicationId);

      if (@ref == null)
      {
        return null;
      }

      var docElement = Doc.GetElement(@ref.ApplicationGeneratedId);

      if (docElement != null)
      {
        return docElement;
      }

      return null;
    }

    #endregion

    #region Project Base Point
    private class BetterBasePoint
    {
      public double X { get; set; } = 0;
      public double Y { get; set; } = 0;
      public double Z { get; set; } = 0;
      public double Angle { get; set; } = 0;
    }

    ////////////////////////////////////////////////
    /// NOTE
    ////////////////////////////////////////////////
    /// The BasePoint in Revit is a mess!
    /// First of all, a BP with coordinates (0,0,0) 
    /// doesn't always, correspond with Revit's absolute origin (0,0,0)
    /// In a brand new file it seems they correspond, but after changing 
    /// the BP values a few times it'll jump somewhere else, try and see yourself.
    /// When it happens the BP symbol in a Revit site view will not be located at (0,0,0)
    /// even if all its values are set to 0. This issue *should not* affect our code,
    /// it just drives you crazy when you don't know it!
    /// Secondly, there are various ways to access the BP values form the API
    /// We are using a FilteredElementCollector .... bla bla ... (BuiltInCategory.OST_ProjectBasePoint)
    /// because Doc.ActiveProjectLocation.GetProjectPosition() always returns an Elevation = 0
    /// WHY?!
    /// Rant end
    ////////////////////////////////////////////////


    private BetterBasePoint _basePoint;
    private BetterBasePoint BasePoint
    {
      get
      {
        if (_basePoint == null)
        {
          var bp = new FilteredElementCollector(Doc).WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_ProjectBasePoint)).FirstOrDefault() as BasePoint;
          if (bp == null)
          {
            _basePoint = new BetterBasePoint();
          }
          else
          {
            _basePoint = new BetterBasePoint
            {
              X = bp.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM).AsDouble(),
              Y = bp.get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM).AsDouble(),
              Z = bp.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM).AsDouble(),
              Angle = bp.get_Parameter(BuiltInParameter.BASEPOINT_ANGLETON_PARAM).AsDouble()
            };
          }
        }
        return _basePoint;
      }
    }

    /// <summary>
    /// For exporting out of Revit, moves and rotates a point according to this document BasePoint
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public XYZ ToExternalCoordinates(XYZ p)
    {
      p = new XYZ(p.X - BasePoint.X, p.Y - BasePoint.Y, p.Z - BasePoint.Z);
      //rotation
      double centX = (p.X * Math.Cos(-BasePoint.Angle)) - (p.Y * Math.Sin(-BasePoint.Angle));
      double centY = (p.X * Math.Sin(-BasePoint.Angle)) + (p.Y * Math.Cos(-BasePoint.Angle));

      XYZ newP = new XYZ(centX, centY, p.Z);

      return newP;
    }

    /// <summary>
    /// For importing in Revit, moves and rotates a point according to this document BasePoint
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public XYZ ToInternalCoordinates(XYZ p)
    {
      //rotation
      double centX = (p.X * Math.Cos(BasePoint.Angle)) - (p.Y * Math.Sin(BasePoint.Angle));
      double centY = (p.X * Math.Sin(BasePoint.Angle)) + (p.Y * Math.Cos(BasePoint.Angle));

      XYZ newP = new XYZ(centX + BasePoint.X, centY + BasePoint.Y, p.Z + BasePoint.Z);

      return newP;
    }
    #endregion
  }
}
