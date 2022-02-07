using System.Text.RegularExpressions;

using Objects.Other;
using Speckle.Core.Kits;
using Speckle.Core.Models;

using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
#if (CIVIL2021 || CIVIL2022)
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

    #region units
    private string _modelUnits;
    public string ModelUnits
    {
      get
      {
        if (string.IsNullOrEmpty(_modelUnits))
        {
          _modelUnits = UnitToSpeckle(Doc.Database.Insunits);

#if (CIVIL2021 || CIVIL2022)
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

    public DisplayStyle GetStyle(DBObject obj)
    {
      var style = new DisplayStyle();
      Entity entity = obj as Entity;

      try
      {
        // get color
        int color = System.Drawing.Color.Black.ToArgb();
        switch (entity.Color.ColorMethod)
        {
          case ColorMethod.ByLayer:
            using(Transaction tr = Doc.Database.TransactionManager.StartTransaction())
            {
              if (entity.LayerId.IsValid)
              {
                var layer = tr.GetObject(entity.LayerId, OpenMode.ForRead)as LayerTableRecord;
                color = layer.Color.ColorValue.ToArgb();
              }
              tr.Commit();
            }
            break;
          case ColorMethod.ByBlock:
          case ColorMethod.ByAci:
          case ColorMethod.ByColor:
            color = entity.Color.ColorValue.ToArgb();
            break;
        }
        style.color = color;

        // get linetype
        style.linetype = entity.Linetype;
        if (entity.Linetype.ToUpper() == "BYLAYER")
        {
          using(Transaction tr = Doc.Database.TransactionManager.StartTransaction())
          {
            if (entity.LayerId.IsValid)
            {
              var layer = tr.GetObject(entity.LayerId, OpenMode.ForRead)as LayerTableRecord;
              var linetype = (LinetypeTableRecord)tr.GetObject(layer.LinetypeObjectId, OpenMode.ForRead);
              style.linetype = linetype.Name;
            }
            tr.Commit();
          }
        }

        // get lineweight
        try
        {
          double lineWeight = 0.25;
          switch (entity.LineWeight)
          {
            case LineWeight.ByLayer:
              using(Transaction tr = Doc.Database.TransactionManager.StartTransaction())
              {
                if (entity.LayerId.IsValid)
                {
                  var layer = tr.GetObject(entity.LayerId, OpenMode.ForRead)as LayerTableRecord;
                  if (layer.LineWeight == LineWeight.ByLineWeightDefault || layer.LineWeight == LineWeight.ByBlock)
                    lineWeight = (int)LineWeight.LineWeight025;
                  else
                    lineWeight = (int)layer.LineWeight;
                }
                tr.Commit();
              }
              break;
            case LineWeight.ByBlock:
            case LineWeight.ByLineWeightDefault:
            case LineWeight.ByDIPs:
              lineWeight = (int)LineWeight.LineWeight025;
              break;
            default:
              lineWeight = (int)entity.LineWeight;
              break;
          }
          style.lineweight = lineWeight / 100; // convert to mm
        }
        catch { }

        return style;
      }
      catch
      {
        return null;
      }
    }
  }
}
