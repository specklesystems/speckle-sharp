using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Objects.Converter.Dynamo
{
  public partial class ConverterDynamo
  {
    private string _modelUnits;
    public string ModelUnits
    {
      get
      {
        if (string.IsNullOrEmpty(_modelUnits))
        {
#if REVIT
          _modelUnits = GetRevitDocUnits();
#else
          _modelUnits = Speckle.Core.Kits.Units.Meters;
#endif
        }
        return _modelUnits;
      }
    }



    private double ScaleToNative(double value, string units)
    {
      var f = Speckle.Core.Kits.Units.GetConversionFactor(units, ModelUnits);
      return value * f;
    }






  }
}
