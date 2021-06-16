using Speckle.Core.Kits;
using Speckle.Core.Models;

using Objects.Other;

using Autodesk.AutoCAD.DatabaseServices;
using System.Drawing;
using System.Text.RegularExpressions;
using Autodesk.AutoCAD.Colors;

namespace Objects.Converter.AutocadCivil
{
  public partial class ConverterAutocadCivil
  {
    #region units
    private string _modelUnits;
    public string ModelUnits
    {
      get
      {
        if (string.IsNullOrEmpty(_modelUnits))
          _modelUnits = UnitToSpeckle(Doc.Database.Insunits);
        return _modelUnits;
      }
    }
    private void SetUnits(Base geom)
    {
      geom.units = ModelUnits;
    }

    private double ScaleToNative(double value, string units)
    {
      var f = Units.GetConversionFactor(units, ModelUnits);
      return value * f;
    }

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
          return Units.Inches;
        case UnitsValue.Feet:
          return Units.Feet;
        case UnitsValue.Yards:
          return Units.Yards;
        case UnitsValue.Miles:
          return Units.Miles;
        default:
          throw new System.Exception("The current Unit System is unsupported.");
      }
    }
    #endregion

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
            using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
            {
              if (entity.LayerId.IsValid)
              {
                var layer = tr.GetObject(entity.LayerId, OpenMode.ForRead) as LayerTableRecord;
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
        if (entity.Linetype == "BYLAYER")
        {
          using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
          {
            if (entity.LayerId.IsValid)
            {
              var layer = tr.GetObject(entity.LayerId, OpenMode.ForRead) as LayerTableRecord;
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
              using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
              {
                if (entity.LayerId.IsValid)
                {
                  var layer = tr.GetObject(entity.LayerId, OpenMode.ForRead) as LayerTableRecord;
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
