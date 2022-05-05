using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Objects.Other;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DB = Autodesk.Revit.DB;
using ElementType = Autodesk.Revit.DB.ElementType;
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

      if (!hostedElementIds.Any())
        return;

      var elementIndex = ContextObjects.FindIndex(obj => obj.applicationId == host.UniqueId);
      if (elementIndex != -1)
      {
        ContextObjects.RemoveAt(elementIndex);
      }

      foreach (var elemId in hostedElementIds)
      {
        var element = host.Document.GetElement(elemId);
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
        if (@base["elements"] == null || !(@base["elements"] is List<Base>))
          @base["elements"] = new List<Base>();

        (@base["elements"] as List<Base>).AddRange(convertedHostedElements);
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

          if (!CanConvertToNative(obj)) continue;

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
          catch (Exception e)
          {
            throw (new Exception($"Failed to create hosted element {obj.speckle_type} in host ({host.Id}): \n{e.Message}"));
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
    /// Adds Instance and Type parameters, ElementId, ApplicationInternalName and Units.
    /// </summary>
    /// <param name="speckleElement"></param>
    /// <param name="revitElement"></param>
    /// <param name="exclusions">List of BuiltInParameters or GUIDs used to indicate what parameters NOT to get,
    /// we exclude all params already defined on the top level object to avoid duplication and 
    /// potential conflicts when setting them back on the element</param>
    public void GetAllRevitParamsAndIds(Base speckleElement, DB.Element revitElement, List<string> exclusions = null)
    {
      var instParams = GetInstanceParams(revitElement, exclusions);
      var typeParams = speckleElement is Level ? null : GetTypeParams(revitElement);  //ignore type props of levels..!
      var allParams = new Dictionary<string, Parameter>();

      if (instParams != null)
        instParams.ToList().ForEach(x => { if (!allParams.ContainsKey(x.Key)) allParams.Add(x.Key, x.Value); });

      if (typeParams != null)
        typeParams.ToList().ForEach(x => { if (!allParams.ContainsKey(x.Key)) allParams.Add(x.Key, x.Value); });

      //sort by key
      allParams = allParams.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
      Base paramBase = new Base();

      foreach (var kv in allParams)
      {
        try
        {
          paramBase[kv.Key] = kv.Value;
        }
        catch
        {
          //ignore
        }
      }

      if (paramBase.GetDynamicMembers().Any())
        speckleElement["parameters"] = paramBase;
      speckleElement["elementId"] = revitElement.Id.ToString();
      speckleElement.applicationId = revitElement.UniqueId;
      speckleElement["units"] = ModelUnits;
    }

    //private List<string> alltimeExclusions = new List<string> { 
    //  "ELEM_CATEGORY_PARAM" };
    private Dictionary<string, Parameter> GetInstanceParams(DB.Element element, List<string> exclusions)
    {
      return GetElementParams(element, false, exclusions);
    }
    private Dictionary<string, Parameter> GetTypeParams(DB.Element element)
    {
      var elementType = element.Document.GetElement(element.GetTypeId());

      if (elementType == null || elementType.Parameters == null)
      {
        return new Dictionary<string, Parameter>();
      }
      return GetElementParams(elementType, true);

    }

    private Dictionary<string, Parameter> GetElementParams(DB.Element element, bool isTypeParameter = false, List<string> exclusions = null)
    {
      exclusions = (exclusions != null) ? exclusions : new List<string>();

      //exclude parameters that don't have a value and those pointing to other elements as we don't support them
      var revitParameters = element.Parameters.Cast<DB.Parameter>()
        .Where(x => x.HasValue && x.StorageType != StorageType.ElementId && !exclusions.Contains(GetParamInternalName(x))).ToList();

      //exclude parameters that failed to convert
      var speckleParameters = revitParameters.Select(x => ParameterToSpeckle(x, isTypeParameter))
        .Where(x => x != null);

      return speckleParameters.GroupBy(x => x.applicationInternalName).Select(x => x.First()).ToDictionary(x => x.applicationInternalName, x => x);
    }

    private T GetParamValue<T>(DB.Element elem, BuiltInParameter bip)
    {
      var rp = elem.get_Parameter(bip);

      if (rp == null || !rp.HasValue)
        return default;

      var value = ParameterToSpeckle(rp).value;
      if (typeof(T) == typeof(int) && value.GetType() == typeof(bool))
        return (T)Convert.ChangeType(value, typeof(int));
      else
        return (T)ParameterToSpeckle(rp).value;
    }

    //rp must HaveValue
    private Parameter ParameterToSpeckle(DB.Parameter rp, bool isTypeParameter = false)
    {
      var sp = new Parameter
      {
        name = rp.Definition.Name,
        applicationInternalName = GetParamInternalName(rp),
        isShared = rp.IsShared,
        isReadOnly = rp.IsReadOnly,
        isTypeParameter = isTypeParameter,
        applicationUnitType = rp.GetUnityTypeString() //eg UT_Length
      };

      switch (rp.StorageType)
      {
        case StorageType.Double:
          // NOTE: do not use p.AsDouble() as direct input for unit utils conversion, it doesn't work.  ¯\_(ツ)_/¯
          var val = rp.AsDouble();
          try
          {
            sp.applicationUnit = rp.GetDisplayUnityTypeString(); //eg DUT_MILLIMITERS, this can throw!
            sp.value = RevitVersionHelper.ConvertFromInternalUnits(val, rp);
          }
          catch
          {
            sp.value = val;
          }
          break;
        case StorageType.Integer:
#if REVIT2023

          if (rp.Definition.GetDataType() == SpecTypeId.Boolean.YesNo)
            sp.value = Convert.ToBoolean(rp.AsInteger());
          else
            sp.value = rp.AsInteger();
#else
          switch (rp.Definition.ParameterType)
          {
            case ParameterType.YesNo:
              sp.value = Convert.ToBoolean(rp.AsInteger());
              break;
            default:
              sp.value = rp.AsInteger();
              break;
          }
#endif
          break;
        case StorageType.String:
          sp.value = rp.AsString();
          if (sp.value == null)
            sp.value = rp.AsValueString();
          break;
        // case StorageType.ElementId:
        //   // NOTE: if this collects too much garbage, maybe we can ignore it
        //   var id = rp.AsElementId();
        //   var e = Doc.GetElement(id);
        //   if (e != null && CanConvertToSpeckle(e))
        //     sp.value = ConvertToSpeckle(e);
        //   break;
        default:
          return null;
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

      var speckleParameters = speckleElement["parameters"] as Base;
      if (speckleParameters == null || speckleParameters.GetDynamicMemberNames().Count() == 0)
        return;

      // NOTE: we are using the ParametersMap here and not Parameters, as it's a much smaller list of stuff and 
      // Parameters most likely contains extra (garbage) stuff that we don't need to set anyways
      // so it's a much faster conversion. If we find that's not the case, we might need to change it in the future
      var revitParameters = revitElement.ParametersMap.Cast<DB.Parameter>().Where(x => x != null && !x.IsReadOnly);

      // Here we are creating two  dictionaries for faster lookup
      // one uses the BuiltInName / GUID the other the name as Key
      // we need both to support parameter set by Schema Builder, that might be generated with one or the other
      // Also, custom parameters that are not Shared, will have an INVALID BuiltInParameter name and no GUID, then we need to use their name
      var revitParameterById = revitParameters.ToDictionary(x => GetParamInternalName(x), x => x);
      var revitParameterByName = revitParameters.ToDictionary(x => x.Definition.Name, x => x);

      // speckleParameters is a Base
      // its member names will have for Key either a BuiltInName, GUID or Name of the parameter (depending onwhere it comes from)
      // and as value the full Parameter object, that might come from Revit or SchemaBuilder
      // We only loop params we can set and that actually exist on the revit element
      var filteredSpeckleParameters = speckleParameters.GetMembers()
        .Where(x => revitParameterById.ContainsKey(x.Key) || revitParameterByName.ContainsKey(x.Key));


      foreach (var spk in filteredSpeckleParameters)
      {
        var sp = spk.Value as Parameter;
        if (sp == null || sp.isReadOnly)
          continue;

        var rp = revitParameterById.ContainsKey(spk.Key) ? revitParameterById[spk.Key] : revitParameterByName[spk.Key];
        try
        {
          switch (rp.StorageType)
          {
            case StorageType.Double:
              // This is meant for parameters that come from Revit
              // as they might use a lot more unit types that Speckle doesn't currently support
              if (!string.IsNullOrEmpty(sp.applicationUnit))
              {
                var val = RevitVersionHelper.ConvertToInternalUnits(sp);
                rp.Set(val);
              }
              // the following two cases are for parameters comimg form schema builder
              // they do not have applicationUnit but just units
              // units are automatically set but the user can override them 
              // users might set them to "none" so that we convert them by using the Revit destination parameter display units
              // this is needed to correctly receive non lenght based parameters (eg air flow)
              else if (sp.units == Speckle.Core.Kits.Units.None)
              {
                var val = RevitVersionHelper.ConvertToInternalUnits(Convert.ToDouble(sp.value), rp);
                rp.Set(val);
              }
              else if (Speckle.Core.Kits.Units.IsUnitSupported(sp.units))
              {
                var val = ScaleToNative(Convert.ToDouble(sp.value), sp.units);
                rp.Set(val);
              }
              else
              {
                rp.Set(Convert.ToDouble(sp.value));
              }
              break;

            case StorageType.Integer:
              rp.Set(Convert.ToInt32(sp.value));
              break;

            case StorageType.String:
              if (rp.Definition.Name.ToLower().Contains("name"))
              {
                var temp = Regex.Replace(Convert.ToString(sp.value), "[^0-9a-zA-Z ]+", "");
                Report.ConversionLog.Add($@"Invalid characters in param name '{rp.Definition.Name}': Renamed to '{temp}'");
                rp.Set(temp);
              }
              else
              {
                rp.Set(Convert.ToString(sp.value));
              }
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

    //Shared parameters use a GUID to be uniquely identified
    //Other parameters use a BuiltInParameter enum
    private string GetParamInternalName(DB.Parameter rp)
    {
      if (rp.IsShared)
        return rp.GUID.ToString();
      else
      {
        var def = rp.Definition as InternalDefinition;
        if (def.BuiltInParameter == BuiltInParameter.INVALID)
          return def.Name;
        return def.BuiltInParameter.ToString();
      }
    }

    //private bool IsValid(DB.Parameter rp)
    //{
    //  if (rp.IsShared)
    //    return true;
    //  else
    //    return (rp.Definition as InternalDefinition).BuiltInParameter != ;
    //}

    private void TrySetParam(DB.Element elem, BuiltInParameter bip, DB.Element value)
    {
      var param = elem.get_Parameter(bip);
      if (param != null && value != null && !param.IsReadOnly)
      {
        param.Set(value.Id);
      }
    }

    private void TrySetParam(DB.Element elem, BuiltInParameter bip, bool value)
    {
      var param = elem.get_Parameter(bip);
      if (param != null && !param.IsReadOnly)
      {
        param.Set(value ? 1 : 0);

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
          param.Set(value);
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
        Report.Log($"Missing type [{family} - {type}] was replaced with [{match.FamilyName} - {match.Name}]");
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
        Report.Log($"Missing family and type, the following family and type were used: {match.FamilyName} - {match.Name}");
        if (match != null)
        {
          if (match is FamilySymbol fs && !fs.IsActive)
          {
            fs.Activate();
          }

          return (T)(object)match;
        }
      }

      throw new Speckle.Core.Logging.SpeckleException($"Could not find any family symbol to use.");
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
        var name = string.IsNullOrEmpty(element["category"].ToString()) ? typeof(T).Name : element["category"].ToString();
        throw new Speckle.Core.Logging.SpeckleException($"Could not find any family to use for category {name}.");
      }

      var family = element["family"] as string;
      var type = element["type"] as string;

      ElementType match = null;

      //if (family == null && type == null)
      //{
      //  match = types.First();
      //}

      if (!string.IsNullOrEmpty(family) && !string.IsNullOrEmpty(type))
      {
        match = types.FirstOrDefault(x => x.FamilyName == family && x.Name == type);
      }

      //some elements only have one family so we didn't add such prop our schema
      if (match == null && string.IsNullOrEmpty(family) && !string.IsNullOrEmpty(type))
      {
        match = types.FirstOrDefault(x => x.Name == type);
      }

      if (match == null && !string.IsNullOrEmpty(family)) // try and match the family only.
      {
        match = types.FirstOrDefault(x => x.FamilyName == family);
        if (match != null) //inform user that the type is different!
          Report.Log($"Missing type. Family: {family} Type: {type}\nType was replaced with: {match.FamilyName}, {match.Name}");

      }
      if (match == null) // okay, try something!
      {
        if (element is BuiltElements.Wall) // specifies the basic wall sub type as default
          match = types.Cast<WallType>().Where(o => o.Kind == WallKind.Basic).Cast<ElementType>().FirstOrDefault();
        if (match == null)
          match = types.First();
        Report.Log($"Missing type. Family: {family} Type: {type}\nType was replaced with: {match.FamilyName}, {match.Name}");
      }

      if (match is FamilySymbol fs && !fs.IsActive)
      {
        fs.Activate();
      }

      return (T)(object)match;
    }

    private ElementFilter GetCategoryFilter(Base element)
    {
      switch (element)
      {
        case BuiltElements.Wall _:
          return new ElementMulticategoryFilter(Categories.wallCategories);
        case Column _:
          return new ElementMulticategoryFilter(Categories.columnCategories);
        case Beam _:
        case Brace _:
          return new ElementMulticategoryFilter(Categories.beamCategories);
        case Duct _:
          return new ElementMulticategoryFilter(Categories.ductCategories);
        case Floor _:
          return new ElementMulticategoryFilter(Categories.floorCategories);
        case Pipe _:
          return new ElementMulticategoryFilter(Categories.pipeCategories);
        case Roof _:
          return new ElementCategoryFilter(BuiltInCategory.OST_Roofs);
        default:
          ElementFilter filter = null;
          if (element["category"] != null)
          {
            var cat = Doc.Settings.Categories.Cast<Category>().FirstOrDefault(x => x.Name == element["category"].ToString());
            if (cat != null)
              filter = new ElementMulticategoryFilter(new List<ElementId> { cat.Id });
          }
          return filter;
      }
    }

    #endregion

    #region conversion "edit existing if possible" utilities

    /// <summary>
    /// Returns, if found, the corresponding doc element.
    /// The doc object can be null if the user deleted it. 
    /// </summary>
    /// <param name="applicationId">Id of the application that originally created the element, in Revit it's the UniqueId</param>
    /// <returns>The element, if found, otherwise null</returns>
    public DB.Element GetExistingElementByApplicationId(string applicationId)
    {
      if (applicationId == null || ReceiveMode == Speckle.Core.Kits.ReceiveMode.Create)
        return null;

      var @ref = PreviousContextObjects.FirstOrDefault(o => o.applicationId == applicationId);

      Element element = null;
      if (@ref == null)
      {
        //element was not cached in a PreviousContex but might exist in the model
        //eg: user sends some objects, moves them, receives them 
        element = Doc.GetElement(applicationId);
      }
      else
      {

        //return the cached object, if it's still in the model
        element = Doc.GetElement(@ref.ApplicationGeneratedId);
      }

      return element;
    }

    #endregion

    #region Reference Point

    // CAUTION: these strings need to have the same values as in the connector bindings
    const string InternalOrigin = "Internal Origin (default)";
    const string ProjectBase = "Project Base";
    const string Survey = "Survey";

    private DB.Transform _transform;
    private DB.Transform ReferencePointTransform
    {
      get
      {
        if (_transform == null)
        {
          // get from settings
          var referencePointSetting = Settings.ContainsKey("reference-point") ? Settings["reference-point"] : string.Empty;
          _transform = GetReferencePointTransform(referencePointSetting);
        }
        return _transform;
      }
    }

    ////////////////////////////////////////////////
    /// NOTE
    ////////////////////////////////////////////////
    /// The BasePoint shared properties in Revit are based off of the survey point.
    /// The BasePoint non-shared properties are based off of the internal origin.
    /// Also, survey point does NOT have an rotation parameter.
    ////////////////////////////////////////////////
    private DB.Transform GetReferencePointTransform(string type)
    {
      // get the correct base point from settings
      var referencePointTransform = DB.Transform.Identity;

      var points = new FilteredElementCollector(Doc).OfClass(typeof(BasePoint)).Cast<BasePoint>().ToList();
      var projectPoint = points.Where(o => o.IsShared == false).FirstOrDefault();
      var surveyPoint = points.Where(o => o.IsShared == true).FirstOrDefault();

      switch (type)
      {
        case ProjectBase:
          if (projectPoint != null)
          {
#if REVIT2019
            var point = projectPoint.get_BoundingBox(null).Min;
#else
            var point = projectPoint.Position;
#endif
            referencePointTransform = DB.Transform.CreateTranslation(point); // rotation to base point is registered by survey point
          }
          break;
        case Survey:
          if (surveyPoint != null)
          {
#if REVIT2019
            var point = surveyPoint.get_BoundingBox(null).Min;
#else
            var point = surveyPoint.Position;
#endif
            var angle = projectPoint.get_Parameter(BuiltInParameter.BASEPOINT_ANGLETON_PARAM).AsDouble(); // !! retrieve survey point angle from project base point
            referencePointTransform = DB.Transform.CreateTranslation(point).Multiply(DB.Transform.CreateRotation(XYZ.BasisZ, angle));
          }
          break;
        default:
          break;
      }

      return referencePointTransform;
    }

    /// <summary>
    /// For exporting out of Revit, moves and rotates a point according to this document BasePoint
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public XYZ ToExternalCoordinates(XYZ p, bool isPoint)
    {
      return (isPoint) ? ReferencePointTransform.Inverse.OfPoint(p) : ReferencePointTransform.Inverse.OfVector(p);
    }

    /// <summary>
    /// For importing in Revit, moves and rotates a point according to this document BasePoint
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public XYZ ToInternalCoordinates(XYZ p, bool isPoint)
    {
      return (isPoint) ? ReferencePointTransform.OfPoint(p) : ReferencePointTransform.OfVector(p);
    }
    #endregion

    #region Floor/ceiling/roof openings

    //a floor/roof/ceiling outline can have "voids/holes" for 3 reasons:
    // - there is a shaft cutting through it > we don't need to create an opening (the shaft will be created on its own)
    // - there is a vertical opening cutting through it > we don't need to create an opening (the opening will be created on its own)
    // - the floor profile was modeled with holes > we need to create an openeing as the Revit API doesn't let us generate it with holes!
    private void CreateVoids(DB.Element host, Base speckleElement)
    {
      if (speckleElement["voids"] == null || !(speckleElement["voids"] is List<ICurve>))
        return;

      //list of openings hosted in this speckle element
      var openings = new List<RevitOpening>();
      if (speckleElement["elements"] != null && (speckleElement["elements"] is List<Base> elements))
        openings.AddRange(elements.Where(x => x is RevitVerticalOpening).Cast<RevitVerticalOpening>());

      //list of shafts part of this conversion set
      openings.AddRange(ContextObjects.Where(x => x.NativeObject is RevitShaft).Select(x => x.NativeObject).Cast<RevitShaft>());

      foreach (var @void in speckleElement["voids"] as List<ICurve>)
      {
        if (HasOverlappingOpening(@void, openings))
          continue;

        var curveArray = CurveToNative(@void);
        Doc.Create.NewOpening(host, curveArray, false);
      }
    }

    private bool HasOverlappingOpening(ICurve @void, List<RevitOpening> openings)
    {
      foreach (RevitOpening opening in openings)
      {
        if (CurvesOverlap(@void, opening.outline))
          return true;
      }
      return false;

    }

    private bool CurvesOverlap(ICurve icurveA, ICurve icurveB)
    {
      var curveArrayA = CurveToNative(icurveA).Cast<DB.Curve>().ToList();
      var curveArrayB = CurveToNative(icurveB).Cast<DB.Curve>().ToList();

      //we need to account for various scenarios, eg a shaft might be made of multiple shapes
      //while the resulting cut in the floor will only be made on a single shape, so we need to cross check them all
      foreach (var curveA in curveArrayA)
      {
        //move curves to Z = 0, needed for shafts!
        curveA.MakeBound(0, 1);
        var z = curveA.GetEndPoint(0).Z;
        var cA = curveA.CreateTransformed(DB.Transform.CreateTranslation(new XYZ(0, 0, -z)));

        foreach (var curveB in curveArrayB)
        {
          //move curves to Z = 0, needed for shafts!
          curveB.MakeBound(0, 1);
          z = curveB.GetEndPoint(0).Z;
          var cB = curveB.CreateTransformed(DB.Transform.CreateTranslation(new XYZ(0, 0, -z)));

          var result = cA.Intersect(cB);
          if (result != SetComparisonResult.BothEmpty && result != SetComparisonResult.Disjoint)
            return true;

        }
      }

      return false;
    }

    #endregion

    #region misc

    public string GetTemplatePath(string templateName)
    {
      var directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Speckle", "Kits", "Objects", "Templates", "Revit", RevitVersionHelper.Version);
      string templatePath = "";
      switch (Doc.DisplayUnitSystem)
      {
        case DisplayUnit.IMPERIAL:
          templatePath = Path.Combine(directoryPath, $"{templateName} - Imperial.rft");
          break;
        case DisplayUnit.METRIC:
          templatePath = Path.Combine(directoryPath, $"{templateName} - Metric.rft");
          break;
      }

      return templatePath;
    }
    #endregion

    private List<ICurve> GetProfiles(DB.SpatialElement room)
    {
      var profiles = new List<ICurve>();
      var boundaries = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
      foreach (var loop in boundaries)
      {
        var poly = new Polycurve(ModelUnits);
        foreach (var segment in loop)
        {
          var c = segment.GetCurve();

          if (c == null)
          {
            continue;
          }

          poly.segments.Add(CurveToSpeckle(c));
        }
        profiles.Add(poly);
      }
      return profiles;
    }

    public WallLocationLine GetWallLocationLine(LocationLine location)
    {
      switch (location)
      {
        case LocationLine.Centerline:
          return WallLocationLine.WallCenterline;
        case LocationLine.Exterior:
          return WallLocationLine.FinishFaceExterior;
        case LocationLine.Interior:
          return WallLocationLine.FinishFaceInterior;
        default:
          return WallLocationLine.FinishFaceInterior;
      }
    }

    #region materials
    public RenderMaterial GetElementRenderMaterial(DB.Element element)
    {
      var matId = element.GetMaterialIds(false).FirstOrDefault();

      if (matId == null)
      {
        // TODO: Fallback to display color or something? 
        return null;
      }

      var revitMaterial = element.Document.GetElement(matId) as Material;
      return RenderMaterialToSpeckle(revitMaterial);
    }

    public static RenderMaterial RenderMaterialToSpeckle(Material revitMaterial)
    {
      if (revitMaterial == null)
        return null;
      RenderMaterial material = new RenderMaterial()
      {
        name = revitMaterial.Name,
        opacity = 1 - (revitMaterial.Transparency / 100d),
        //metalness = revitMaterial.Shininess / 128d, //Looks like these are not valid conversions
        //roughness = 1 - (revitMaterial.Smoothness / 100d),
        diffuse = System.Drawing.Color.FromArgb(revitMaterial.Color.Red, revitMaterial.Color.Green, revitMaterial.Color.Blue).ToArgb()
      };

      return material;
    }

    public ElementId RenderMaterialToNative(RenderMaterial speckleMaterial)
    {
      if (speckleMaterial == null) return ElementId.InvalidElementId;

      // Try and find an existing material
      var existing = new FilteredElementCollector(Doc)
        .OfClass(typeof(Material))
        .Cast<Material>()
        .FirstOrDefault(m => string.Equals(m.Name, speckleMaterial.name, StringComparison.CurrentCultureIgnoreCase));

      if (existing != null) return existing.Id;

      // Create new material
      ElementId materialId = DB.Material.Create(Doc, speckleMaterial.name);
      Material mat = Doc.GetElement(materialId) as Material;

      var sysColor = System.Drawing.Color.FromArgb(speckleMaterial.diffuse);
      mat.Color = new DB.Color(sysColor.R, sysColor.G, sysColor.B);
      mat.Transparency = (int)((1d - speckleMaterial.opacity) * 100d);

      return materialId;
    }

    /// <summary>
    /// Retrieves the material from assigned system type for mep elements
    /// </summary>
    /// <param name="e">Revit element to parse</param>
    /// <returns></returns>
    public static RenderMaterial GetMEPSystemMaterial(Element e)
    {
      ElementId idType = ElementId.InvalidElementId;

      if (e is DB.MEPCurve dt)
      {
        var system = dt.MEPSystem;
        if (system != null)
        {
          idType = system.GetTypeId();
        }
      }
      else if (IsSupportedMEPCategory(e))
      {
        MEPModel m = ((DB.FamilyInstance)e).MEPModel;

        if (m != null && m.ConnectorManager != null)
        {
          //retrieve the first material from first connector. Could go wrong, but better than nothing ;-)
          foreach (Connector item in m.ConnectorManager.Connectors)
          {
            var system = item.MEPSystem;
            if (system != null)
            {
              idType = system.GetTypeId();
              break;
            }
          }
        }
      }

      if (idType == ElementId.InvalidElementId) return null;

      if (e.Document.GetElement(idType) is MEPSystemType mechType)
      {
        var mat = e.Document.GetElement(mechType.MaterialId) as Material;
        RenderMaterial material = RenderMaterialToSpeckle(mat);

        return material;
      }

      return null;
    }

    private static bool IsSupportedMEPCategory(Element e)
    {
      var categories = e.Document.Settings.Categories;

      var supportedCategories = new[]
      {
        BuiltInCategory.OST_PipeFitting,
        BuiltInCategory.OST_DuctFitting,
        BuiltInCategory.OST_DuctAccessory,
        BuiltInCategory.OST_PipeAccessory,
        //BuiltInCategory.OST_MechanicalEquipment,
      };

      return supportedCategories.Any(cat => e.Category.Id == categories.get_Item(cat).Id);
    }

    #endregion
  }
}
