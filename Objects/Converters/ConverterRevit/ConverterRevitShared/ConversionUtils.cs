using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB;
using ConverterRevitShared.Extensions;
using Objects.BuiltElements.Revit;
using Objects.Converter.Revit.Models;
using Objects.Geometry;
using Objects.Other;
using RevitSharedResources.Interfaces;
using Speckle.Core.Helpers;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using Speckle.Core.Models;
using Speckle.Core.Models.Extensions;
using Speckle.Core.Models.GraphTraversal;
using DB = Autodesk.Revit.DB;
using Level = Objects.BuiltElements.Level;
using Line = Objects.Geometry.Line;
using Parameter = Objects.BuiltElements.Revit.Parameter;
using Point = Objects.Geometry.Point;

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
      if (ConvertedObjects.Contains(element.UniqueId))
      {
        return false;
      }

      // the parent is in our selection list,skip it, as this element will be converted by the host element
      if (ContextObjects.ContainsKey(host.UniqueId))
      {
        return false;
      }
      return true;
    }

    private bool ShouldConvertHostedElement(DB.Element element, DB.Element host, Base extraProps)
    {
      // doesn't have a host that will convert the element, go ahead and do it now
      if (host == null || host is DB.Level)
        return true;

      // has been converted before (from a parent host), skip it
      if (ConvertedObjects.Contains(element.UniqueId))
        return false;

      // the parent is in our selection list,skip it, as this element will be converted by the host element
      if (ContextObjects.ContainsKey(host.UniqueId))
      {
        // there are certain elements in Revit that can be a host to another element
        // yet not know it.
        var hostedElementIds = GetHostedElementIds(host);
        var elementId = element.Id;
        if (!hostedElementIds.Contains(elementId))
        {
          extraProps["speckleHost"] = new Base()
          {
            applicationId = host.UniqueId,
            ["category"] = host.Category.Name,
            ["builtInCategory"] = Categories.GetBuiltInCategory(host.Category)
          };
        }
        else
          return false;
      }
      return true;
    }

    /// <summary>
    /// Gets the hosted element of a host and adds the to a Base object
    /// </summary>
    /// <param name="host"></param>
    /// <param name="base"></param>
    public void GetHostedElements(Base @base, Element host, out List<string> notes)
    {
      notes = new List<string>();
      var hostedElementIds = GetHostedElementIds(host);

      if (!hostedElementIds.Any())
        return;

      if (ContextObjects.ContainsKey(host.UniqueId))
      {
        ContextObjects.Remove(host.UniqueId);
      }
      GetHostedElementsFromIds(@base, host, hostedElementIds, out notes);
    }

    public void GetHostedElementsFromIds(
      Base @base,
      Element host,
      IList<ElementId> hostedElementIds,
      out List<string> notes
    )
    {
      notes = new List<string>();
      var convertedHostedElements = new List<Base>();

      foreach (var elemId in hostedElementIds)
      {
        var element = host.Document.GetElement(elemId);
        if (!ContextObjects.ContainsKey(element.UniqueId))
        {
          continue;
        }

        var reportObj = Report.ReportObjects.TryGetValue(element.UniqueId, out ApplicationObject value)
          ? value
          : new ApplicationObject(element.UniqueId, element.GetType().ToString());

        if (CanConvertToSpeckle(element))
        {
          try
          {
            var obj = ConvertToSpeckle(element);
            if (obj != null)
            {
              ContextObjects.Remove(element.UniqueId);
              reportObj.Update(
                status: ApplicationObject.State.Created,
                logItem: $"Attached as hosted element to {host.UniqueId}"
              );
              convertedHostedElements.Add(obj);
              ConvertedObjects.Add(obj.applicationId);
            }
            else
            {
              reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"Conversion returned null");
            }
          }
          catch (Exception ex)
          {
            SpeckleLog.Logger.Error(ex, ex.Message);
            reportObj.Update(status: ApplicationObject.State.Failed, logItem: $"Conversion threw exception: {ex}");
          }
        }
        else
        {
          reportObj.Update(status: ApplicationObject.State.Skipped, logItem: $"Conversion not supported");
        }
        Report.Log(reportObj);
      }

      if (convertedHostedElements.Any())
      {
        notes.Add($"Converted and attached {convertedHostedElements.Count} hosted elements");

        if (@base.GetDetachedProp("elements") is List<Base> elements)
        {
          elements.AddRange(convertedHostedElements);
        }
        else
        {
          @base.SetDetachedProp("elements", convertedHostedElements);
        }
      }
    }

    public IList<ElementId> GetHostedElementIds(Element host)
    {
      IList<ElementId> ids = null;
      if (host is HostObject hostObject)
      {
        ids = hostObject.FindInserts(true, false, false, false);
      }
      else
      {
        var typeFilter = new ElementIsElementTypeFilter(true);
        var categoryFilter = new ElementMulticategoryFilter(
          new List<BuiltInCategory>()
          {
            BuiltInCategory.OST_CLines,
            BuiltInCategory.OST_SketchLines,
            BuiltInCategory.OST_WeakDims
          },
          true
        );
        ids = host.GetDependentElements(new LogicalAndFilter(typeFilter, categoryFilter));
      }

      // dont include host elementId
      ids.Remove(host.Id);

      return ids;
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
      var allParams = new Dictionary<string, Parameter>();
      AddElementParamsToDict(revitElement, allParams, false, exclusions);

      var elementType = revitElement.Document.GetElement(revitElement.GetTypeId());
      AddElementParamsToDict(
        speckleElement is Level ? null : elementType, //ignore type props of levels..!
        allParams,
        true,
        exclusions
      );

      Base paramBase = new();
      //sort by key
      foreach (var kv in allParams.OrderBy(x => x.Key))
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
      speckleElement["isRevitLinkedModel"] = revitElement.Document.IsLinked;
      speckleElement["revitLinkedModelPath"] = revitElement.Document.PathName;

      Phase phaseCreated = Doc.GetElement(revitElement.CreatedPhaseId) as Phase;
      if (phaseCreated != null)
        speckleElement["phaseCreated"] = phaseCreated.Name;

      Phase phaseDemolished = Doc.GetElement(revitElement.DemolishedPhaseId) as Phase;
      if (phaseDemolished != null)
        speckleElement["phaseDemolished"] = phaseDemolished.Name;

      speckleElement["worksetId"] = revitElement.WorksetId.ToString();

      // assign the category if it is null
      // WARN: DirectShapes have a `category` prop of type `RevitCategory` (enum), NOT `string`. This is the only exception as of 2.16.
      // In all other cases this should be the display value string (localized name) of the catogory
      // If the null check is removed, the DirectShape case needs to be handled.
      var category = revitElement.Category;
      if (speckleElement["category"] is null && category is not null)
      {
        speckleElement["category"] = category.Name;
      }
      // from 2.16 onward we're also passing the full BuiltInCategory for better handling on receive
      //TODO: move this to a typed property, define full list of categories in Objects
      BuiltInCategory builtInCategory = Categories.GetBuiltInCategory(category);
      speckleElement["builtInCategory"] = builtInCategory.ToString();

      //NOTE: adds the quantities of all materials to an element
      var qs = MaterialQuantitiesToSpeckle(revitElement, speckleElement["units"] as string);
      if (qs != null)
        speckleElement["materialQuantities"] = qs;
    }

    private void AddElementParamsToDict(
      DB.Element element,
      Dictionary<string, Parameter> paramDict,
      bool isTypeParameter = false,
      List<string> exclusions = null
    )
    {
      if (element == null)
        return;

      exclusions ??= new();
      using var parameters = element.Parameters;
      foreach (DB.Parameter param in parameters)
      {
        var internalName = GetParamInternalName(param);
        if (paramDict.ContainsKey(internalName) || exclusions.Contains(internalName))
        {
          continue;
        }

        var speckleParam = ParameterToSpeckle(param, internalName, isTypeParameter);
        paramDict[internalName] = speckleParam;
      }
    }

    /// <summary>
    /// Returns the value of a Revit Built-In <see cref="DB.Parameter"/> given a target <see cref="DB.Element"/> and <see cref="BuiltInParameter"/>
    /// </summary>
    /// <param name="elem">The <see cref="DB.Element"/> containing the Built-In <see cref="DB.Parameter"/></param>
    /// <param name="bip">The <see cref="BuiltInParameter"/> enum name of the target parameter</param>
    /// <param name="unitsOverride">The units in which to return the value in the case where you want to override the Built-In <see cref="DB.Parameter"/>'s units</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T GetParamValue<T>(DB.Element elem, BuiltInParameter bip, string unitsOverride = null)
    {
      var rp = elem.get_Parameter(bip);

      if (rp == null || !rp.HasValue)
        return default;

      var value = rp.GetValue(rp.Definition, unitsOverride);
      if (typeof(T) == typeof(int) && value.GetType() == typeof(bool))
        return (T)Convert.ChangeType(value, typeof(int));

      return (T)value;
    }

    /// <summary>
    /// Converts a Revit Built-In <see cref="DB.Parameter"/> to a Speckle <see cref="Parameter"/>.
    /// </summary>
    /// <param name="rp">The Revit Built-In <see cref="DB.Parameter"/> to convert</param>
    /// <param name="isTypeParameter">Defaults to false. True if this is a type parameter</param>
    /// <param name="unitsOverride">The units in which to return the value in the case where you want to override the Built-In <see cref="DB.Parameter"/>'s units</param>
    /// <returns></returns>
    private Parameter ParameterToSpeckle(
      DB.Parameter rp,
      string paramInternalName,
      bool isTypeParameter = false,
      string unitsOverride = null
    )
    {
#if REVIT2020
      DisplayUnitType unitTypeId = default;
#else
      ForgeTypeId unitTypeId = null;
#endif

      // The parameter definitions are cached using the ParameterToSpeckleData struct
      // This is done because in the case of type and instance parameter there is lots of redundant data that needs to be extracted from the Revit DB
      // Caching noticeably speeds up the send process
      // TODO : could add some generic getOrAdd overloads to avoid creating closures
      var paramData = revitDocumentAggregateCache
        .GetOrInitializeEmptyCacheOfType<ParameterToSpeckleData>(out _)
        .GetOrAdd(
          paramInternalName,
          () =>
          {
            var definition = rp.Definition;
            var newParamData = new ParameterToSpeckleData()
            {
              Definition = definition,
              InternalName = paramInternalName,
              IsReadOnly = rp.IsReadOnly,
              IsShared = rp.IsShared,
              IsTypeParameter = isTypeParameter,
              Name = definition.Name,
              UnitType = definition.GetUnityTypeString(),
            };
            if (rp.StorageType == StorageType.Double)
            {
              unitTypeId = rp.GetUnitTypeId();
              newParamData.UnitsSymbol = GetSymbolUnit(rp, definition, unitTypeId);
              newParamData.ApplicationUnits =
                unitsOverride != null ? UnitsToNative(unitsOverride).ToUniqueString() : unitTypeId.ToUniqueString();
            }
            return newParamData;
          },
          out _
        );

      return paramData.GetParameterObjectWithValue(rp.GetValue(paramData.Definition, unitTypeId));
    }

    #endregion

    /// <summary>
    /// Method for getting symbol when parameter is NOT validated to be a double or int
    /// </summary>
    /// <param name="parameter"></param>
    /// <param name="definition"></param>
    /// <param name="cache"></param>
    /// <param name="forgeTypeId"></param>
    /// <returns></returns>
    public string GetSymbolUnit(DB.Parameter parameter, DB.Definition definition,
#if REVIT2020
      DisplayUnitType unitTypeId
#else
      ForgeTypeId unitTypeId
#endif
    )
    {
      if (parameter.StorageType != StorageType.Double)
      {
        return null;
      }

      return revitDocumentAggregateCache
        .GetOrInitializeEmptyCacheOfType<string>(out _)
        .GetOrAdd(unitTypeId.ToUniqueString(), () => unitTypeId.GetSymbol(), out _);
    }

    /// <summary>
    /// </summary>
    /// <param name="revitElement"></param>
    /// <param name="speckleElement"></param>
    public void SetInstanceParameters(Element revitElement, Base speckleElement, List<string> exclusions = null)
    {
      if (revitElement == null)
        return;

      var speckleParameters = speckleElement["parameters"] as Base;
      if (speckleParameters == null || speckleParameters.GetDynamicMemberNames().Count() == 0)
        return;

      // Set the phaseCreated parameter
      if (speckleElement["phaseCreated"] is string phaseCreated && !string.IsNullOrEmpty(phaseCreated))
        TrySetParam(revitElement, BuiltInParameter.PHASE_CREATED, GetRevitPhase(revitElement.Document, phaseCreated));
      //Set the phaseDemolished parameter
      if (speckleElement["phaseDemolished"] is string phaseDemolished && !string.IsNullOrEmpty(phaseDemolished))
        TrySetParam(
          revitElement,
          BuiltInParameter.PHASE_DEMOLISHED,
          GetRevitPhase(revitElement.Document, phaseDemolished)
        );

      // NOTE: we are using the ParametersMap here and not Parameters, as it's a much smaller list of stuff and
      // Parameters most likely contains extra (garbage) stuff that we don't need to set anyways
      // so it's a much faster conversion. If we find that's not the case, we might need to change it in the future
      IEnumerable<DB.Parameter> revitParameters = null;
      if (exclusions == null)
        revitParameters = revitElement.ParametersMap.Cast<DB.Parameter>().Where(x => x != null && !x.IsReadOnly);
      else
        revitParameters = revitElement.ParametersMap
          .Cast<DB.Parameter>()
          .Where(x => x != null && !x.IsReadOnly && !exclusions.Contains(GetParamInternalName(x)));

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
      var filteredSpeckleParameters = speckleParameters
        .GetMembers()
        .Where(x => revitParameterById.ContainsKey(x.Key) || revitParameterByName.ContainsKey(x.Key));

      foreach (var spk in filteredSpeckleParameters)
      {
        if (!(spk.Value is Parameter sp) || sp.isReadOnly || sp.value == null)
          continue;

        var rp = revitParameterById.ContainsKey(spk.Key) ? revitParameterById[spk.Key] : revitParameterByName[spk.Key];

        TrySetParam(rp, sp.value, applicationUnit: sp.applicationUnit);
      }
    }

    private void TrySetParam(DB.Parameter rp, object value, string units = "", string applicationUnit = "")
    {
      try
      {
        switch (rp.StorageType)
        {
          case StorageType.Double:
            // This is meant for parameters that come from Revit
            // as they might use a lot more unit types that Speckle doesn't currently support
            if (!string.IsNullOrEmpty(applicationUnit))
            {
              var val = RevitVersionHelper.ConvertToInternalUnits(value, applicationUnit);
              rp.Set(val);
            }
            // the following two cases are for parameters comimg form schema builder
            // they do not have applicationUnit but just units
            // units are automatically set but the user can override them
            // users might set them to "none" so that we convert them by using the Revit destination parameter display units
            // this is needed to correctly receive non lenght based parameters (eg air flow)
            else if (units == Speckle.Core.Kits.Units.None)
            {
              var val = RevitVersionHelper.ConvertToInternalUnits(Convert.ToDouble(value), rp);
              rp.Set(val);
            }
            else if (Speckle.Core.Kits.Units.IsUnitSupported(units))
            {
              var val = ScaleToNative(Convert.ToDouble(value), units);
              rp.Set(val);
            }
            else
            {
              rp.Set(Convert.ToDouble(value));
            }
            break;

          case StorageType.Integer:
            if (value is string s)
            {
              if (s.ToLower() == "no")
              {
                value = 0;
              }
              else if (s.ToLower() == "yes")
              {
                value = 1;
              }
            }
            rp.Set(Convert.ToInt32(value));
            break;

          case StorageType.String:
            if (rp.Definition.Name.ToLower().Contains("name"))
            {
              var temp = Regex.Replace(Convert.ToString(value), "[^0-9a-zA-Z ]+", "");
              Report.Log($@"Invalid characters in param name '{rp.Definition.Name}': Renamed to '{temp}'");
              rp.Set(temp);
            }
            else
            {
              rp.Set(Convert.ToString(value));
            }
            break;
          default:
            break;
        }
      }
      catch
      {
        // do nothing for now...
      }
    }

    //Shared parameters use a GUID to be uniquely identified
    //Other parameters use a BuiltInParameter enum
    private static string GetParamInternalName(DB.Parameter rp)
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

    private void TrySetParam(DB.Element elem, BuiltInParameter bip, object value, string units = "")
    {
      var param = elem.get_Parameter(bip);
      if (param == null || param.IsReadOnly)
      {
        return;
      }

      TrySetParam(param, value, units);
    }

    /// <summary>
    /// Queries a Revit Document for phases by the given name.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="phaseName">The name of the Phase</param>
    /// <returns>the phase which has the same name. null if none or multiple phases were found.</returns>
    private Phase GetRevitPhase(DB.Document document, string phaseName)
    {
      // cache the phases if we haven't already done so
      if (Phases.Count == 0)
      {
        var phases = new FilteredElementCollector(document).OfCategory(BuiltInCategory.OST_Phases).ToList();
        foreach (var phase in phases)
        {
          Phases[phase.Name] = phase as Phase;
        }
      }
      if (Phases.ContainsKey(phaseName))
        return Phases[phaseName];

      return null;
    }

    #endregion

    #region conversion "edit existing if possible" utilities

    /// <summary>
    /// Returns, if found, the corresponding doc element.
    /// The doc object can be null if the user deleted it.
    /// </summary>
    /// <param name="applicationId">Id of the application that originally created the element, in Revit it's the UniqueId</param>
    /// <returns>The element, if found, otherwise null</returns>
    public DB.Element? GetExistingElementByApplicationId(string applicationId)
    {
      if (applicationId == null || ReceiveMode == Speckle.Core.Kits.ReceiveMode.Create)
        return null;

      var cachedIds = PreviouslyReceivedObjectIds?.GetCreatedIdsFromConvertedId(applicationId);
      // TODO: we may not want just the first one
      return cachedIds == null ? null : Doc.GetElement(cachedIds.First());
    }

    public IEnumerable<DB.Element?> GetExistingElementsByApplicationId(string applicationId)
    {
      if (applicationId == null || ReceiveMode == ReceiveMode.Create)
        yield break;

      var cachedIds = PreviouslyReceivedObjectIds?.GetCreatedIdsFromConvertedId(applicationId);
      if (cachedIds == null)
        yield break;
      foreach (var id in cachedIds)
        yield return Doc.GetElement(id);
    }

    /// <summary>
    /// Returns true if element is not null and the user-selected receive mode is set to "ignore"
    /// </summary>
    /// <param name="docObj">Existing document element</param>
    /// <param name="appObj"></param>
    /// <param name="updatedAppObj">The updated appObj if method returns true, the original appObj if false</param>
    /// <returns></returns>
    public bool IsIgnore(Element docObj, ApplicationObject appObj)
    {
      if (docObj != null)
      {
        if (ReceiveMode == ReceiveMode.Ignore)
        {
          appObj.Update(
            status: ApplicationObject.State.Skipped,
            createdId: docObj.UniqueId,
            convertedItem: docObj,
            logItem: $"ApplicationId already exists in document, new object ignored."
          );
          return true;
        }
        else if (docObj.Pinned)
        {
          appObj.Update(
            status: ApplicationObject.State.Skipped,
            createdId: docObj.UniqueId,
            convertedItem: docObj,
            logItem: "Element is pinned and cannot be updated"
          );
          return true;
        }
      }
      return false;
    }
    #endregion

    #region Reference Point

    // CAUTION: these strings need to have the same values as in the connector bindings
    const string InternalOrigin = "Internal Origin (default)";
    const string ProjectBase = "Project Base";
    const string Survey = "Survey";

    //cached during conversion
    private List<RevitLinkInstance> _revitLinkInstances = null;
    private List<RevitLinkInstance> RevitLinkInstances
    {
      get
      {
        if (_revitLinkInstances == null)
          _revitLinkInstances = new FilteredElementCollector(Doc)
            .OfClass(typeof(RevitLinkInstance))
            .ToElements()
            .Cast<RevitLinkInstance>()
            .ToList();

        return _revitLinkInstances;
      }
    }

    private Dictionary<string, DB.Transform> _docTransforms = new Dictionary<string, DB.Transform>();

    private DB.Transform GetDocReferencePointTransform(Document doc)
    {
      //linked files are always saved to disc and will have a path name
      //if the current doc is unsaved it will not, but then it'll be the only one :)
      var id = doc.PathName;

      if (!_docTransforms.ContainsKey(id))
      {
        // get from settings
        var referencePointSetting = Settings.ContainsKey("reference-point")
          ? Settings["reference-point"]
          : string.Empty;
        _docTransforms[id] = GetReferencePointTransform(referencePointSetting, doc);
      }

      return _docTransforms[id];
    }

    ////////////////////////////////////////////////
    /// NOTE
    ////////////////////////////////////////////////
    /// The BasePoint shared properties in Revit are based off of the survey point.
    /// The BasePoint non-shared properties are based off of the internal origin.
    /// Also, survey point does NOT have an rotation parameter.
    ////////////////////////////////////////////////
    private DB.Transform GetReferencePointTransform(string type, Document doc)
    {
      // first get the main doc base points and reference setting transform
      var referencePointTransform = DB.Transform.Identity;
      var points = new FilteredElementCollector(Doc).OfClass(typeof(BasePoint)).Cast<BasePoint>().ToList();
      var projectPoint = points.FirstOrDefault(o => o.IsShared == false);
      var surveyPoint = points.FirstOrDefault(o => o.IsShared == true);
      switch (type)
      {
        case ProjectBase: // note that the project base (ui) rotation is registered on the survey pt, not on the base point
          referencePointTransform = DB.Transform.CreateTranslation(projectPoint.Position);
          break;
        case Survey:
          // note that the project base (ui) rotation is registered on the survey pt, not on the base point
          // retrieve the survey point rotation from the project point
          var angle = projectPoint.get_Parameter(BuiltInParameter.BASEPOINT_ANGLETON_PARAM)?.AsDouble() ?? 0;
          referencePointTransform = DB.Transform
            .CreateTranslation(surveyPoint.Position)
            .Multiply(DB.Transform.CreateRotation(XYZ.BasisZ, angle));
          break;
        case InternalOrigin:
          break;
      }

      // Second, if this is a linked doc get the transform and adjust
      if (doc.IsLinked)
      {
        // get the linked doc instance transform
        var instance = RevitLinkInstances.FirstOrDefault(x => x?.GetLinkDocument()?.PathName == doc.PathName);
        if (instance != null)
        {
          var linkInstanceTransform = instance.GetTotalTransform();
          referencePointTransform = linkInstanceTransform.Inverse.Multiply(referencePointTransform);
        }
      }

      return referencePointTransform;
    }

    /// <summary>
    /// For exporting out of Revit, moves and rotates a point according to this document BasePoint
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public XYZ ToExternalCoordinates(XYZ p, bool isPoint, Document doc)
    {
      var rpt = GetDocReferencePointTransform(doc);
      return (isPoint) ? rpt.Inverse.OfPoint(p) : rpt.Inverse.OfVector(p);
    }

    /// <summary>
    /// For importing in Revit, moves and rotates a point according to this document BasePoint
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public XYZ ToInternalCoordinates(XYZ p, bool isPoint)
    {
      var rpt = GetDocReferencePointTransform(Doc);
      return (isPoint) ? rpt.OfPoint(p) : rpt.OfVector(p);
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
      //we used to use "elements" but have now switched to "@elements"
      //this extra check is for backwards compatibility
      var nestedElements = @speckleElement["elements"] ?? @speckleElement["@elements"];
      if (nestedElements is List<Base> elements)
        openings.AddRange(elements.Where(x => x is RevitVerticalOpening).Cast<RevitVerticalOpening>());

      //list of shafts part of this conversion set
      var shafts = ContextObjects.Values
        .SelectMany(x => x.Converted.Where(y => y is RevitShaft).Cast<RevitShaft>())
        .ToList();

      openings.AddRange(shafts);

      foreach (var @void in speckleElement["voids"] as List<ICurve>)
      {
        if (HasOverlappingOpening(@void, openings))
          continue;

        var curveArray = CurveToNative(@void, true);
        UnboundCurveIfSingle(curveArray);
        Doc.Create.NewOpening(host, curveArray, false);
      }
    }

    private bool HasOverlappingOpening(ICurve @void, List<RevitOpening> openings)
    {
      foreach (RevitOpening opening in openings)
        if (CurvesOverlap(@void, opening.outline))
          return true;

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
      var directoryPath = Path.Combine(
        SpecklePathProvider.ObjectsFolderPath,
        "Templates",
        "Revit",
        RevitVersionHelper.Version
      );
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

    public IEnumerable<(string, Element, Connector)> GetRevitConnectorsThatConnectToSpeckleConnector(
      RevitMEPConnector revitMEPConnector,
      IConvertedObjectsCache<Base, Element> receivedObjectsCache
    )
    {
      var origin = PointToNative(revitMEPConnector.origin);

      foreach (var connectedId in revitMEPConnector.connectedConnectorIds)
      {
        var connectorAppId = connectedId.Split('.').First();
        var convertedElement = receivedObjectsCache.GetCreatedObjectsFromConvertedId(connectorAppId).FirstOrDefault();

        var existingRevitConnector = convertedElement
          ?.GetConnectorSet()
          .Where(c => c.Origin.DistanceTo(origin) < .01)
          .FirstOrDefault();

        yield return (connectorAppId, convertedElement, existingRevitConnector);
      }
    }

    public void CreateSystemConnections(
      IEnumerable<RevitMEPConnector> revitMEPConnectors,
      Element revitEl,
      IConvertedObjectsCache<Base, Element> receivedObjectsCache
    )
    {
      foreach (var speckleConnector in revitMEPConnectors)
      {
        var origin = PointToNative(speckleConnector.origin);
        var newRevitConnector = revitEl
          .GetConnectorSet()
          .Where(c => c.Origin.DistanceTo(origin) < .01)
          .FirstOrDefault();

        if (newRevitConnector == null)
          continue;

        foreach (
          var (elementAppId, element, existingConnector) in GetRevitConnectorsThatConnectToSpeckleConnector(
            speckleConnector,
            receivedObjectsCache
          )
        )
        {
          existingConnector?.ConnectTo(newRevitConnector);
        }
      }
    }

    public T TryInSubtransaction<T>(Func<T> func, Action<Exception> catchFunc)
    {
      using var subtransaction = new SubTransaction(Doc);
      subtransaction.Start();

      T returnValue = default;
      try
      {
        returnValue = func();
        subtransaction.Commit();
      }
      catch (Exception ex)
      {
        subtransaction.RollBack();
        Doc.Regenerate();
        catchFunc(ex);
      }
      return returnValue;
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
            continue;

          var curve = CurveToSpeckle(c, room.Document);

          ((Base)curve)["elementId"] = segment.ElementId.ToString();

          poly.segments.Add(curve);
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
      var matId = element?.GetMaterialIds(false)?.FirstOrDefault();

      if (matId == null)
      {
        // TODO: Fallback to display color or something?
        return null;
      }

      var revitMaterial = element.Document.GetElement(matId) as DB.Material;
      return RenderMaterialToSpeckle(revitMaterial);
    }

    public static RenderMaterial RenderMaterialToSpeckle(DB.Material revitMaterial)
    {
      if (revitMaterial == null)
        return null;
      RenderMaterial material = new RenderMaterial()
      {
        name = revitMaterial.Name,
        opacity = 1 - (revitMaterial.Transparency / 100d),
        //metalness = revitMaterial.Shininess / 128d, //Looks like these are not valid conversions
        //roughness = 1 - (revitMaterial.Smoothness / 100d),
        diffuse = System.Drawing.Color
          .FromArgb(revitMaterial.Color.Red, revitMaterial.Color.Green, revitMaterial.Color.Blue)
          .ToArgb()
      };

      return material;
    }

    public ElementId RenderMaterialToNative(RenderMaterial speckleMaterial)
    {
      if (speckleMaterial == null)
        return ElementId.InvalidElementId;

      string matName = RemoveProhibitedCharacters(speckleMaterial.name);

      // Try and find an existing material
      var existing = new FilteredElementCollector(Doc)
        .OfClass(typeof(DB.Material))
        .Cast<DB.Material>()
        .FirstOrDefault(m => string.Equals(m.Name, matName, StringComparison.CurrentCultureIgnoreCase));

      if (existing != null)
        return existing.Id;

      // Create new material
      ElementId materialId = DB.Material.Create(Doc, matName ?? Guid.NewGuid().ToString());
      DB.Material mat = Doc.GetElement(materialId) as DB.Material;

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
      DB.Material material = GetMEPSystemRevitMaterial(e);
      return material != null ? RenderMaterialToSpeckle(material) : null;
    }

    /// <summary>
    /// Retrieves the revit material from assigned system type for mep elements
    /// </summary>
    /// <param name="e">Revit element to parse</param>
    /// <returns>Revit material of the element, null if no material found</returns>
    public static DB.Material GetMEPSystemRevitMaterial(Element e)
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

      if (idType == ElementId.InvalidElementId)
        return null;

      if (e.Document.GetElement(idType) is MEPSystemType mechType)
      {
        return e.Document.GetElement(mechType.MaterialId) as DB.Material;
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


    /// <summary>
    /// Checks if a Speckle <see cref="Line"/> is too sort to be created in Revit.
    /// </summary>
    /// <remarks>
    /// The length of the line will be computed on the spot to ensure it is accurate.
    /// </remarks>
    /// <param name="line">The <see cref="Line"/> to be tested.</param>
    /// <returns>true if the line is too short, false otherwise.</returns>
    public bool IsLineTooShort(Line line)
    {
      var scaleToNative = ScaleToNative(Point.Distance(line.start, line.end), line.units);
      return scaleToNative < Doc.Application.ShortCurveTolerance;
    }

    /// <summary>
    /// Attempts to append a Speckle <see cref="Line"/> onto a Revit <see cref="CurveArray"/>.
    /// This method ensures the line is long enough to be supported.
    /// It will also convert the line to Revit before appending it to the <see cref="CurveArray"/>.
    /// </summary>
    /// <param name="curveArray">The revit <see cref="CurveArray"/> to add the line to.</param>
    /// <param name="line">The <see cref="Line"/> to be added.</param>
    /// <returns>True if the line was added, false otherwise.</returns>
    public bool TryAppendLineSafely(CurveArray curveArray, Line line, ApplicationObject appObj)
    {
      if (IsLineTooShort(line))
      {
        appObj.Log.Add(
          "Some lines in the CurveArray where ignored due to being smaller than the allowed curve length."
        );
        return false;
      }
      try
      {
        curveArray.Append(LineToNative(line));
        return true;
      }
      catch (Exception e)
      {
        appObj.Log.Add(e.Message);
        return false;
      }
    }

    public bool UnboundCurveIfSingle(DB.CurveArray array)
    {
      if (array.Size != 1)
        return false;
      var item = array.get_Item(0);
      if (!item.IsBound)
        return false;
      item.MakeUnbound();
      return true;
    }

    public bool IsCurveClosed(DB.Curve nativeCurve, double tol = 1E-6)
    {
      var endPoint = nativeCurve.GetEndPoint(0);
      var source = nativeCurve.GetEndPoint(1);
      var distanceTo = endPoint.DistanceTo(source);
      return distanceTo < tol;
    }

    public (DB.Curve, DB.Curve) SplitCurveInTwoHalves(DB.Curve nativeCurve)
    {
      var curveArray = new CurveArray();
      // Revit does not like single curve loop edges, so we split them in two.
      var start = nativeCurve.GetEndParameter(0);
      var end = nativeCurve.GetEndParameter(1);
      var mid = start + ((end - start) / 2);

      var a = nativeCurve.Clone();
      a.MakeBound(start, mid);
      curveArray.Append(a);
      var b = nativeCurve.Clone();
      b.MakeBound(mid, end);
      curveArray.Append(b);

      return (a, b);
    }

    public class FallbackToDxfException : Exception
    {
      public FallbackToDxfException(string message)
        : base(message) { }

      public FallbackToDxfException(string message, Exception innerException)
        : base(message, innerException) { }
    }

    public ApplicationObject CheckForExistingObject(Base @base)
    {
      @base.applicationId ??= @base.id;

      var docObj = GetExistingElementByApplicationId(@base.applicationId);
      var appObj = new ApplicationObject(@base.id, @base.speckle_type) { applicationId = @base.applicationId };

      // skip if element already exists in doc & receive mode is set to ignore
      if (IsIgnore(docObj, appObj))
        return appObj;

      // otherwise just create new one
      if (docObj != null)
        Doc.Delete(docObj.Id);

      return null;
    }

    private string RemoveProhibitedCharacters(string s)
    {
      if (string.IsNullOrEmpty(s))
        return s;
      return Regex.Replace(s, "[\\[\\]{}|;<>?`~]", "");
    }

    public static ModelLine GetSlopeArrow(Element element)
    {
      IList<ElementId> elementIds = null;
#if !REVIT2020 && !REVIT2021
      if (element is DB.Floor floor)
      {
        elementIds = ((Sketch)floor.Document.GetElement(floor.SketchId)).GetAllElements();
      }
#endif
      if (elementIds == null)
      {
        using var modelLineFilter = new ElementCategoryFilter(BuiltInCategory.OST_SketchLines);
        elementIds = element.GetDependentElements(modelLineFilter);
      }

      foreach (var elementId in elementIds)
      {
        if (element.Document.GetElement(elementId) is not ModelLine line)
          continue;

        var offsetAtTailParameter = line.get_Parameter(BuiltInParameter.SLOPE_START_HEIGHT);
        if (offsetAtTailParameter != null)
        {
          return line;
        }
      }
      return null;
    }

    private Point GetSlopeArrowHead(ModelLine slopeArrow, Document doc)
    {
      if (slopeArrow == null)
        return null;
      return PointToSpeckle(((LocationCurve)slopeArrow.Location).Curve.GetEndPoint(1), doc);
    }

    private Point GetSlopeArrowTail(ModelLine slopeArrow, Document doc)
    {
      if (slopeArrow == null)
        return null;
      return PointToSpeckle(((LocationCurve)slopeArrow.Location).Curve.GetEndPoint(0), doc);
    }

    public static double GetSlopeArrowTailOffset(ModelLine slopeArrow, Document doc)
    {
      return GetParamValue<double>(slopeArrow, BuiltInParameter.SLOPE_START_HEIGHT);
    }

    public static double GetSlopeArrowHeadOffset(
      ModelLine slopeArrow,
      Document doc,
      double tailOffset,
      out double slope
    )
    {
      var specifyOffset = GetParamValue<int>(slopeArrow, BuiltInParameter.SPECIFY_SLOPE_OR_OFFSET);
      var lineLength = GetParamValue<double>(slopeArrow, BuiltInParameter.CURVE_ELEM_LENGTH);

      slope = 0;
      double headOffset = 0;
      // 1 corrosponds to the "slope" option
      if (specifyOffset == 1)
      {
        // in this scenario, slope is returned as a percentage. Divide by 100 to get the unitless form
        slope = GetParamValue<double>(slopeArrow, BuiltInParameter.ROOF_SLOPE) / 100;
        headOffset = tailOffset + lineLength * Math.Sin(Math.Atan(slope));
      }
      else if (specifyOffset == 0) // 0 corrospondes to the "height at tail" option
      {
        headOffset = GetParamValue<double>(slopeArrow, BuiltInParameter.SLOPE_END_HEIGHT);
        slope = (headOffset - tailOffset) / lineLength;
      }

      return headOffset;
    }
  }
}
