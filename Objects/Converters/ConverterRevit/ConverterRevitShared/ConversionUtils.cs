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

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    #region parameters

    /// <summary>
    /// Sets various common properties on a speckle object, where found. Examples: family, type, parameters, type parameters, elementIds, etc.
    /// </summary>
    /// <param name="speckleElement"></param>
    /// <param name="revitElement"></param>
    private void AddCommonRevitProps(Base speckleElement, DB.Element revitElement)
    {
      var parms = GetElementParams(revitElement);
      if (parms != null)
      {
        speckleElement["parameters"] = parms;
      }

      var typeParams = GetElementTypeParams(revitElement);
      if (typeParams != null && !(speckleElement is Level)) //ignore type props of levels..!
      {
        speckleElement["typeParameters"] = typeParams;
      }

      speckleElement["elementId"] = revitElement.Id.ToString();
      speckleElement.applicationId = revitElement.UniqueId;
      speckleElement.units = ModelUnits;
    }

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
            catch (Exception)
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

        if (myParamDict.ContainsKey(keyName))
        {
          continue;
        }

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
            catch (Exception)
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

    public Dictionary<string, object> GetElementTypeParams(DB.Element myElement)
    {
      var myElementType = Doc.GetElement(myElement.GetTypeId());

      if (myElementType == null || myElementType.Parameters == null)
      {
        return null;
      }

      var myParamDict = new Dictionary<string, object>();

      foreach (Parameter p in myElementType.Parameters)
      {
        var keyName = SanitizeKeyname(p.Definition.Name);

        if (myParamDict.ContainsKey(keyName))
        {
          continue;
        }

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
            catch (Exception)
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

    /// <summary>
    /// </summary>
    /// <param name="revitElement"></param>
    /// <param name="spkElement"></param>
    /// <param name="exclusions"></param>
    public void SetInstanceParameters(Element revitElement, Base spkElement, List<string> exclusions = null)
    {
      if (revitElement == null)
        return;

      var paramDictionary = spkElement["parameters"] as Dictionary<string, object>;
      if (paramDictionary == null || !paramDictionary.Any())
        return;

      exclusions = (exclusions != null) ? exclusions : new List<string>();
      var cleanParams = paramDictionary.Where(x => !x.Key.StartsWith("__") && !exclusions.Contains(x.Key));


      var paramMap = revitElement.ParametersMap.Cast<Parameter>().Where(x => x != null && !x.IsReadOnly)
        .ToDictionary(x => x.Definition.Name, x => x);

      foreach (var kvp in cleanParams)
      {
        try
        {
          var keyName = UnsanitizeKeyname(kvp.Key);
          if (!paramMap.ContainsKey(keyName))
            continue;

          //TODO: try support params in foreign language
          var parameter = paramMap[keyName];

          switch (parameter.StorageType)
          {
            case StorageType.Double:
              var hasUnitKey = paramDictionary.ContainsKey("__unitType::" + parameter.Definition.Name);
              if (hasUnitKey)
              {
                var unitType = (string)paramDictionary["__unitType::" + kvp.Key];
                var unit = (string)paramDictionary["__unit::" + kvp.Key];
                DisplayUnitType sourceUnit;
                Enum.TryParse(unit, out sourceUnit);
                var convertedValue = UnitUtils.ConvertToInternalUnits(Convert.ToDouble(kvp.Value), sourceUnit);

                parameter.Set(convertedValue);
              }
              else
              {
                parameter.Set(Convert.ToDouble(kvp.Value));
              }
              break;

            case StorageType.Integer:
              parameter.Set(Convert.ToInt32(kvp.Value));
              break;

            case StorageType.String:
              parameter.Set(Convert.ToString(kvp.Value));
              break;

            case StorageType.ElementId:
              // TODO (OLD) /Fake out: most important element id params should go as props in the object model
              break;
          }
        }
        catch (Exception ex)
        {
        }
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

    public static string SanitizeKeyname(string keyName)
    {
      return keyName.Replace(".", "☞"); // BECAUSE FML
    }

    public static string UnsanitizeKeyname(string keyname)
    {
      return keyname.Replace("☞", ".");
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

      if (family == null && type == null)
      {
        match = types.First();
      }

      if (family != null && type != null)
      {
        match = types.FirstOrDefault(x => x.FamilyName == family && x.Name == type);
      }

      if (match == null && type != null) // try and match the type only
      {
        if (element is Duct)
        {
          match = types.FirstOrDefault(x => x.FamilyName == type);
        }
        else
        {
          match = types.FirstOrDefault(x => x.Name == type);
        }
      }

      if (match == null && family != null) // try and match the family only.
      {
        match = types.FirstOrDefault(x => x.FamilyName == family);
      }

      if (match == null) // okay, try something!
      {
        match = types.First();
        ConversionErrors.Add(new Error($"Missing type. Family: {family} Type:{type}", $"Type was replaced with: {match.FamilyName}, {match.Name}"));
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
  }
}
