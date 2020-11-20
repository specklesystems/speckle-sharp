using Autodesk.Revit.DB;
using Objects.Revit;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

using DB = Autodesk.Revit.DB;

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

    public static string SanitizeKeyname(string keyName)
    {
      return keyName.Replace(".", "☞"); // BECAUSE FML
    }

    public static string UnsanitizeKeyname(string keyname)
    {
      return keyname.Replace("☞", ".");
    }

    private T GetElementType<T>(string family, string type)
    {
      List<ElementType> types = new FilteredElementCollector(Doc).WhereElementIsElementType().OfClass(typeof(T)).ToElements().Cast<ElementType>().ToList();

      //match family and type
      var match = types.FirstOrDefault(x => x.FamilyName == family && x.Name == type);
      if (match != null)
      {
        if (match is FamilySymbol fs && !fs.IsActive)
          fs.Activate();
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
            fs.Activate();
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
            fs.Activate();
          return (T)(object)match;
        }
      }

      throw new Exception($"Could not find any family symbol to use.");
    }

    private T GetElementType<T>(IBuiltElement element)
    {
      List<ElementType> types = new List<ElementType>();
      ElementMulticategoryFilter filter = null;

      if (element is IColumn)
      {
        filter = new ElementMulticategoryFilter(Categories.columnCategories);
      }
      else if (element is IBeam || element is IBrace)
      {
        filter = new ElementMulticategoryFilter(Categories.beamCategories);
      }
      //else if (element is IDuct)
      //{
      //  filter = new ElementMulticategoryFilter(Categories.ductCategories);
      //}

      if (filter != null)
      {
        types = new FilteredElementCollector(Doc).WhereElementIsElementType().OfClass(typeof(T)).WherePasses(filter).ToElements().Cast<ElementType>().ToList();
      }
      else
      {
        types = new FilteredElementCollector(Doc).WhereElementIsElementType().OfClass(typeof(T)).ToElements().Cast<ElementType>().ToList();
      }

      if (element is RevitElement ire)
      {
        //match family and type
        var match = types.FirstOrDefault(x => x.FamilyName == ire.family && x.Name == ire.type);
        if (match != null)
        {
          if (match is FamilySymbol fs && !fs.IsActive)
            fs.Activate();
          return (T)(object)match;
        }

        //match type
        match = types.FirstOrDefault(x => x.FamilyName == ire.family);
        if (match != null)
        {
          ConversionErrors.Add(new Error($"Missing type: {ire.family} {ire.type}", $"Type was replace with: {match.FamilyName} - {match.Name}"));
          if (match != null)
          {
            if (match is FamilySymbol fs && !fs.IsActive)
              fs.Activate();
            return (T)(object)match;
          }
        }
      }

      // get whatever we found, could be a different category!
      if (types.Any())
      {
        var match = types.FirstOrDefault();
        ConversionErrors.Add(new Error($"Missing family and type", $"The following family and type were used: {match.FamilyName} - {match.Name}"));
        if (match != null)
        {
          if (match is FamilySymbol fs && !fs.IsActive)
            fs.Activate();
          return (T)(object)match;
        }
      }

      throw new Exception($"Could not find any family symbol to use.");
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

    private void TrySetParam(DB.Element elem, BuiltInParameter bip, DB.Element value)
    {
      var param = elem.get_Parameter(bip);
      if (param != null && value != null && !param.IsReadOnly)
        param.Set(value.Id);
    }
  }
}