using Autodesk.Revit.DB;
using Objects.BuiltElements;
using Objects.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using DB = Autodesk.Revit.DB;
using Element = Objects.BuiltElements.Element;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {

    private void AddCommonRevitProps(IRevit speckleElement, DB.Element revitElement)
    {

      if (speckleElement is RevitElement speckleRevitElement)
      {
        if (revitElement is DB.FamilyInstance)
        {
          speckleRevitElement.family = (revitElement as DB.FamilyInstance).Symbol.FamilyName;
        }

        if (CanGetElementTypeParams(revitElement))
          speckleRevitElement.typeParameters = GetElementTypeParams(revitElement);
        speckleRevitElement.parameters = GetElementParams(revitElement);
        speckleRevitElement.applicationId = revitElement.UniqueId;
      }

      speckleElement.elementId = revitElement.Id.ToString();
    }
    //TODO: CLEAN THE BELOW 
    /// <summary>
    /// Gets a dictionary representation of all this element's parameters.
    /// TODO: (old) manage (somehow!) units; essentially set them back to whatever the current document
    /// setting is (meters, millimiters, etc). 
    /// </summary>
    /// <param name="myElement"></param>
    /// <returns></returns>
    public Dictionary<string, object> GetElementParams(DB.Element myElement)
    {
      var myParamDict = new Dictionary<string, object>();

      // Get params from the unique list
      foreach (Parameter p in myElement.ParametersMap)
      {
        var keyName = SanitizeKeyname(p.Definition.Name);
        switch (p.StorageType)
        {
          case StorageType.Double:
            // NOTE: do not use p.AsDouble() as direct input for unit utils conversion, it doesn't work.  ¯\_(ツ)_/¯
            var val = p.AsDouble();
            try
            {
              myParamDict[keyName] = UnitUtils.ConvertFromInternalUnits(val, p.DisplayUnitType);
              myParamDict["__unitType::" + keyName] = p.Definition.UnitType.ToString();
              myParamDict["__unit::" + keyName] = p.DisplayUnitType.ToString();

            }
            catch (Exception e)
            {
              myParamDict[keyName] = val;
            }
            break;
          case StorageType.Integer:
            myParamDict[keyName] = p.AsInteger();
            //myParamDict[ keyName ] = UnitUtils.ConvertFromInternalUnits( p.AsInteger(), p.DisplayUnitType);
            break;
          case StorageType.String:
            myParamDict[keyName] = p.AsString();
            break;
          case StorageType.ElementId:
            // TODO: (OLD) Properly get ref elemenet and serialise it in here.
            // NOTE: Too much garbage for too little info...
            //var docEl = Doc.GetElement( p.AsElementId() );
            //var spk = SpeckleCore.Converter.Serialise( docEl );
            //if( !(spk is SpeckleNull) ) {
            //  myParamDict[ keyName + "_el" ] = spk;
            //  myParamDict[ keyName ] = p.AsValueString();
            //} else
            myParamDict[keyName] = p.AsValueString();
            break;
          case StorageType.None:
            break;
        }
      }

      // Get any other parameters from the "big" list
      foreach (Parameter p in myElement.Parameters)
      {
        var keyName = SanitizeKeyname(p.Definition.Name);

        if (myParamDict.ContainsKey(keyName)) continue;
        switch (p.StorageType)
        {
          case StorageType.Double:
            // NOTE: do not use p.AsDouble() as direct input for unit utils conversion, it doesn't work.  ¯\_(ツ)_/¯
            var val = p.AsDouble();
            try
            {
              myParamDict[keyName] = UnitUtils.ConvertFromInternalUnits(val, p.DisplayUnitType);
              myParamDict["__unitType::" + keyName] = p.Definition.UnitType.ToString();
              myParamDict["__unit::" + keyName] = p.DisplayUnitType.ToString();
            }
            catch (Exception e)
            {
              myParamDict[keyName] = val;
            }
            break;
          case StorageType.Integer:
            myParamDict[keyName] = p.AsInteger();
            //myParamDict[ keyName ] = UnitUtils.ConvertFromInternalUnits( p.AsInteger(), p.DisplayUnitType);
            break;
          case StorageType.String:
            myParamDict[keyName] = p.AsString();
            break;
          case StorageType.ElementId:
            myParamDict[keyName] = p.AsValueString();
            break;
          case StorageType.None:
            break;
        }
      }

      //sort parameters
      myParamDict = myParamDict.OrderBy(obj => obj.Key).ToDictionary(obj => obj.Key, obj => obj.Value);


      // myParamDict["__units"] = unitsDict;
      // TODO: (OLD) BIG CORE PROBLEM: failure to serialise things with nested dictionary (like the line above).
      return myParamDict;
    }
    private bool CanGetElementTypeParams(DB.Element element)
    {
      var typeElement = Doc.GetElement(element.GetTypeId());
      if (typeElement == null || typeElement.Parameters == null)
        return false;
      return true;
    }

    public Dictionary<string, object> GetElementTypeParams(DB.Element myElement)
    {
      var myParamDict = new Dictionary<string, object>();

      var myElementType = Doc.GetElement(myElement.GetTypeId());

      foreach (Parameter p in myElementType.Parameters)
      {
        var keyName = SanitizeKeyname(p.Definition.Name);

        if (myParamDict.ContainsKey(keyName)) continue;
        switch (p.StorageType)
        {
          case StorageType.Double:
            // NOTE: do not use p.AsDouble() as direct input for unit utils conversion, it doesn't work.  ¯\_(ツ)_/¯
            var val = p.AsDouble();
            try
            {
              myParamDict[keyName] = UnitUtils.ConvertFromInternalUnits(val, p.DisplayUnitType);
              myParamDict["__unitType::" + keyName] = p.Definition.UnitType.ToString();
              myParamDict["__unit::" + keyName] = p.DisplayUnitType.ToString();
            }
            catch (Exception e)
            {
              myParamDict[keyName] = val;
            }
            break;
          case StorageType.Integer:
            myParamDict[keyName] = p.AsInteger();
            break;
          case StorageType.String:
            myParamDict[keyName] = p.AsString();
            break;
          case StorageType.ElementId:
            myParamDict[keyName] = p.AsValueString();
            break;
          case StorageType.None:
            break;
        }
      }

      //sort parameters
      myParamDict = myParamDict.OrderBy(obj => obj.Key).ToDictionary(obj => obj.Key, obj => obj.Value);


      // myParamDict["__units"] = unitsDict;
      // TODO: (OLD) BIG CORE PROBLEM: failure to serialise things with nested dictionary (like the line above).
      return myParamDict;
    }

    public void SetElementParams(DB.Element myElement, IRevit spkElement, List<string> exclusions = null)
    {

      if (myElement == null) return;
      if (spkElement.parameters == null) return;

      //var questForTheBest = UnitDictionary;

      foreach (var kvp in spkElement.parameters)
      {
        if (kvp.Key.Contains("__unitType::")) continue; // skip unit types please
        if (exclusions != null && exclusions.Contains(kvp.Key)) continue;
        try
        {
          var keyName = UnsanitizeKeyname(kvp.Key);

          //TODO: try support params in foreign language
          var myParam = myElement.ParametersMap.get_Item(keyName);
          if (myParam == null) continue;
          if (myParam.IsReadOnly) continue;

          switch (myParam.StorageType)
          {
            case StorageType.Double:
              var hasUnitKey = spkElement.parameters.ContainsKey("__unitType::" + myParam.Definition.Name);
              if (hasUnitKey)
              {
                var unitType = (string)spkElement.parameters["__unitType::" + kvp.Key];
                var unit = (string)spkElement.parameters["__unit::" + kvp.Key];
                DisplayUnitType sourceUnit;
                Enum.TryParse(unit, out sourceUnit);
                var convertedValue = UnitUtils.ConvertToInternalUnits(Convert.ToDouble(kvp.Value), sourceUnit);

                myParam.Set(convertedValue);
              }
              else
              {
                myParam.Set(Convert.ToDouble(kvp.Value));
              }
              break;

            case StorageType.Integer:
              myParam.Set(Convert.ToInt32(kvp.Value));
              break;

            case StorageType.String:
              myParam.Set(Convert.ToString(kvp.Value));
              break;

            case StorageType.ElementId:
              // TODO (OLD) /Fake out: most important element id params should go as props in the object model
              break;
          }
        }
        catch (Exception e)
        {
        }
      }

    }

    public static string SanitizeKeyname(string keyName)
    {
      return keyName.Replace(".", "☞"); // BECAUSE FML
    }

    public static string UnsanitizeKeyname(string keyname)
    {
      return keyname.Replace("☞", ".");
    }

    /// <summary>
    /// Gets an element by its type and name. If nothing found, returns the first one of that type.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public T GetElementByTypeAndName<T>(string name)
    {
      var collector = new FilteredElementCollector(Doc).OfClass(typeof(T));

      if (string.IsNullOrEmpty(name)) return (T)(object)collector.FirstElement();

      if (name.ToLower().Contains("duct"))
      { // DuctType.Name is just 'Default'
        foreach (DB.Mechanical.DuctType myElement in collector.ToElements())
          if (myElement.FamilyName == name) return (T)(object)myElement;
      }

      foreach (var myElement in collector.ToElements())
        if (myElement.Name == name) return (T)(object)myElement;


      // now returning the first type, which means we didn't find the type we were actually looking for.
      ConversionErrors.Add(new Error($"Missing type: {name}", $"{collector.FirstElement().Name} has been used instead."));
      return (T)(object)collector.FirstElement();
    }

    /// <summary>
    /// Returns, if found, the corresponding doc element and its corresponding local state object.
    /// The doc object can be null if the user deleted it. 
    /// </summary>
    /// <param name="ApplicationId"></param>
    /// <returns></returns>
    public static (DB.Element, Base) GetExistingElementByApplicationId(string ApplicationId, string ObjectType)
    {
      //TODO: uncomment the below
      //foreach (var stream in Revit)
      //{
      //  var found = stream.Objects.FirstOrDefault(s => s.ApplicationId == ApplicationId && (string)s.Properties["__type"] == ObjectType);
      //  if (found != null)
      //    return (Doc.GetElement(found.Properties["revitUniqueId"] as string), (Base)found);
      //}
      return (null, null);
    }

    public static (List<DB.Element>, List<Base>) GetExistingElementsByApplicationId(string ApplicationId, string ObjectType)
    {
      //TODO: uncomment the below
      //var allStateObjects = (from p in Initialiser.LocalRevitState.SelectMany(s => s.Objects) select p).ToList();

      //var found = allStateObjects.Where(obj => obj.ApplicationId == ApplicationId && (string)obj.Properties["__type"] == ObjectType);
      //var revitObjs = found.Select(obj => Doc.GetElement(obj.Properties["revitUniqueId"] as string));

      //return (revitObjs.ToList(), found.ToList());
      return (null, null);
    }



    /// <summary>
    /// Stolen from grevit.
    /// </summary>
    /// <param name="document"></param>
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public DB.Element GetElementByClassAndName(Type type, string name = null)
    {
      var collector = new FilteredElementCollector(Doc).OfClass(type);

      // check against element name
      if (name == null)
        return collector.FirstElement();

      foreach (var e in collector.ToElements())
        if (e.Name == name)
          return e;

      return collector.FirstElement();
    }



    private FamilySymbol GetFamilySymbol(IBuiltElement element)
    {
      List<FamilySymbol> symbols = new List<FamilySymbol>();
      ElementMulticategoryFilter filter = null;

      if (element is IColumn)
      {
        filter = new ElementMulticategoryFilter(Categories.columnCategories);
      }
      else if (element is IBeam || element is IBrace)
      {
        filter = new ElementMulticategoryFilter(Categories.beamCategories);
      }

      if (filter != null)
      {
        symbols = new FilteredElementCollector(Doc).WhereElementIsElementType().OfClass(typeof(FamilySymbol)).WherePasses(filter).ToElements().Cast<FamilySymbol>().ToList();
      }
      else
      {
        symbols = new FilteredElementCollector(Doc).WhereElementIsElementType().OfClass(typeof(FamilySymbol)).ToElements().Cast<FamilySymbol>().ToList();
      }


      if (element is RevitElement ire)
      {
        //match family and type
        var match = symbols.FirstOrDefault(x => x.FamilyName == ire.family && x.Name == ire.type);
        if (match != null)
        {
          if (!match.IsActive) match.Activate();
          return match;
        }

        //match type
        match = symbols.FirstOrDefault(x => x.FamilyName == ire.family);
        if (match != null)
        {
          ConversionErrors.Add(new Error($"Missing type: {ire.family} {ire.type}", $"Type was replace with: {match.FamilyName} - {match.Name}"));
          if (!match.IsActive) match.Activate();
          return match;
        }
      }

      // get whatever we found, could be a different category!
      if (symbols.Any())
      {
        var match = symbols.FirstOrDefault();
        ConversionErrors.Add(new Error($"Missing family and type", $"The following family and type were used: {match.FamilyName} - {match.Name}"));
        if (!match.IsActive) match.Activate();
        return match;
      }

      throw new Exception($"Could not find any family symbol to use.");
    }

    //private void TrySetParam(DB.Element elem, BuiltInParameter bip, double value)
    //{

    //}

    private void TrySetParam(DB.Element elem, BuiltInParameter bip, DB.Element value)
    {
      var param = elem.get_Parameter(bip);
      if (param != null && value != null && !param.IsReadOnly)
        param.Set(value.Id);
    }

  }
}
