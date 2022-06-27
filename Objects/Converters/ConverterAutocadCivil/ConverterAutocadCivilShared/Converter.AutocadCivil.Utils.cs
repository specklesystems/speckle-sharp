using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Models;

using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

#if CIVIL2021 || CIVIL2022 || CIVIL2023
using Autodesk.Aec.ApplicationServices;
#endif

namespace Objects.Converter.AutocadCivil
{
  public static class Utils
  {
    public static BlockTableRecord GetModelSpace(this Database db)
    {
      return (BlockTableRecord)SymbolUtilityServices.GetBlockModelSpaceId(db).GetObject(OpenMode.ForWrite);
    }
    public static ObjectId Append(this BlockTableRecord owner, Entity entity)
    {
      if (!entity.IsNewObject)
        return entity.Id;
      var tr = owner.Database.TransactionManager.TopTransaction;
      var id = owner.AppendEntity(entity);
      tr.AddNewlyCreatedDBObject(entity, true);
      return id;
    }
  }

  public partial class ConverterAutocadCivil
  {
    public static string invalidAutocadChars = @"<>/\:;""?*|=,‘";

    private Dictionary<string, ObjectId> _lineTypeDictionary = new Dictionary<string, ObjectId>();
    public Dictionary<string, ObjectId> LineTypeDictionary
    {
      get
      {
        if (_lineTypeDictionary.Values.Count == 0)
        {
          var lineTypeTable = (LinetypeTable)Trans.GetObject(Doc.Database.LinetypeTableId, OpenMode.ForRead);
          foreach (ObjectId lineTypeId in lineTypeTable)
          {
            var linetype = (LinetypeTableRecord)Trans.GetObject(lineTypeId, OpenMode.ForRead);
            _lineTypeDictionary.Add(linetype.Name, lineTypeId);
          }
        }
        return _lineTypeDictionary;
      }
    }

    /// <summary>
    /// Removes invalid characters for Autocad layer and block names
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string RemoveInvalidAutocadChars(string str)
    {
      // using this to handle rhino nested layer syntax
      // replace "::" layer delimiter with "$" (acad standard)
      string cleanDelimiter = str.Replace("::", "$");

      // remove all other invalid chars
      return Regex.Replace(cleanDelimiter, $"[{invalidAutocadChars}]", string.Empty);
    }

    #region app props
    public static string AutocadPropName = "AutocadProps";

    private Base GetAutoCADProps(DBObject o, Type t, bool getParentProps = false)
    {
      var appProps = new Base();
      appProps["class"] = t.Name;

      // set primitive writeable props 
      foreach (var propInfo in t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
      {
        try
        {
          if (propInfo.GetSetMethod() != null &&
            (propInfo.PropertyType.IsPrimitive ||
            propInfo.PropertyType == typeof(string) ||
            propInfo.PropertyType == typeof(decimal)))
          {
            var propValue = propInfo.GetValue(o);
            if (propInfo.GetValue(o) != null)
              appProps[propInfo.Name] = propValue;
          }
        }
        catch (Exception e)
        { }
      }
      if (getParentProps)
      {
        foreach (var propInfo in t.BaseType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
        {
          try
          {
            if (propInfo.GetSetMethod() != null &&
              (propInfo.PropertyType.IsPrimitive ||
              propInfo.PropertyType == typeof(string) ||
              propInfo.PropertyType == typeof(decimal)))
            {
              var propValue = propInfo.GetValue(o);
              if (propInfo.GetValue(o) != null)
                appProps[propInfo.Name] = propValue;
            }
          }
          catch (Exception e)
          { }
        }
      }

      return appProps;
    }

    // TODO: need to determine when props should be scaled to native units!!
    private void SetAutoCADProps(object o, Type t, Base props)
    {
      var propNames = props.GetDynamicMembers();
      if (o == null || propNames.Count() == 0)
        return;

      foreach (var propInfo in t.GetProperties())
      {
        try
        {
          if (propInfo.CanWrite && propNames.Contains(propInfo.Name))
            t.InvokeMember(propInfo.Name,
              BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
              Type.DefaultBinder, o, new object[] { props[propInfo.Name] });
        }
        catch (Exception e)
        { }
      }
    }
    #endregion

    #region units
    private string _modelUnits;
    public string ModelUnits
    {
      get
      {
        if (string.IsNullOrEmpty(_modelUnits))
        {
          _modelUnits = UnitToSpeckle(Doc.Database.Insunits);

#if CIVIL2021 || CIVIL2022 || CIVIL2023
          if (_modelUnits == Units.None)
          {
            // try to get the drawing unit instead
            using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
            {
              var id = DrawingSetupVariables.GetInstance(Doc.Database, false);
              var setupVariables = (DrawingSetupVariables)tr.GetObject(id, OpenMode.ForRead);
              var linearUnit = setupVariables.LinearUnit;
              _modelUnits = Units.GetUnitsFromString(linearUnit.ToString());
              tr.Commit();
            }
          }
#endif
        }
        return _modelUnits;
      }
    }
    private void SetUnits(Base geom)
    {
      geom["units"] = ModelUnits;
    }

    private double ScaleToNative(double value, string units)
    {
      var f = Units.GetConversionFactor(units, ModelUnits);
      return value * f;
    }

    // Note: Difference between International Foot and US Foot is ~ 0.0000006 as described in: https://www.pobonline.com/articles/98788-us-survey-feet-versus-international-feet
    private string UnitToSpeckle(UnitsValue units)
    {
      switch (units)
      {
        case UnitsValue.Millimeters:
          return Units.Millimeters;
        case UnitsValue.Centimeters:
          return Units.Centimeters;
        case UnitsValue.Meters:
          return Units.Meters;
        case UnitsValue.Kilometers:
          return Units.Kilometers;
        case UnitsValue.Inches:
        case UnitsValue.USSurveyInch:
          return Units.Inches;
        case UnitsValue.Feet:
        case UnitsValue.USSurveyFeet:
          return Units.Feet;
        case UnitsValue.Yards:
        case UnitsValue.USSurveyYard:
          return Units.Yards;
        case UnitsValue.Miles:
        case UnitsValue.USSurveyMile:
          return Units.Miles;
        case UnitsValue.Undefined:
          return Units.None;
        default:
          throw new Speckle.Core.Logging.SpeckleException($"The Unit System \"{units}\" is unsupported.");
      }
    }
    #endregion

    
  }
}
