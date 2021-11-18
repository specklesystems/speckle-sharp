using System;
using System.Collections.Generic;
using System.Text;
using Objects.Structural.Analysis;

namespace Objects.Converter.ETABS
{
  public partial class ConverterETABS
  {
    public object UnitsToNative()
    {
      return null;
    }
    public ModelUnits UnitsToSpeckle()
    {
      var modelUnits = new ModelUnits();
      var units = Model.GetDatabaseUnits();
      if (units != 0)
      {
        string[] unitsCat = units.ToString().Split('_');
        modelUnits.temperature = unitsCat[2];
        modelUnits.length = unitsCat[1];
        modelUnits.force = unitsCat[0];
      }
      else
      {
        //TO DO: custom units
      }
      return modelUnits;
    }
  }
}
