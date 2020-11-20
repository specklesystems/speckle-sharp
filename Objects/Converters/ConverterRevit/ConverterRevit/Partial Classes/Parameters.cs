using Autodesk.Revit.DB;
using Objects.Revit;
using System;
using System.Collections.Generic;
using System.Linq;

using DB = Autodesk.Revit.DB;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
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
  }
}