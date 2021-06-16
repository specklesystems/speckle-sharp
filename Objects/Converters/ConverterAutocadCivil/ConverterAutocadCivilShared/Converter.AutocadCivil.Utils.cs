using Speckle.Core.Kits;
using Speckle.Core.Models;

using Objects.Other;

using Autodesk.AutoCAD.DatabaseServices;
using System.Drawing;
using System.Text.RegularExpressions;

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

      style.color = entity.Color.ColorValue.ToArgb();
      style.linetype = entity.Linetype;

      // get lineweight
      double lineWeight = 0;
      switch (entity.LineWeight)
      {
        case LineWeight.ByLayer:
          using (Transaction tr = Doc.Database.TransactionManager.StartTransaction())
          {
            var layer = tr.GetObject(entity.LayerId, OpenMode.ForRead) as LayerTableRecord;
            lineWeight = (int)layer.LineWeight / 100;
            tr.Commit();
          }
          break;
        case LineWeight.ByBlock:
        case LineWeight.ByLineWeightDefault:
        case LineWeight.ByDIPs:
          lineWeight = (int)LineWeight.LineWeight000;
          break;
        default:
          lineWeight = (int)entity.LineWeight / 100; // this should be mm
          break;
      }
      style.lineweight = lineWeight;

      return style;
    }
  }
}
