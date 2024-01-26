using Objects.Structural.Analysis;
using CSiAPIv1;

namespace Objects.Converter.CSI;

public partial class ConverterCSI
{
  public void UnitsToNative(ModelUnits units)
  {
    eForce force = units.force switch
    {
      "N" => eForce.N,
      "kip" => eForce.kip,
      "kN" => eForce.kN,
      "lb" => eForce.lb,
      "tf" => eForce.tonf,
      _ => eForce.NotApplicable
    };

    eLength length = units.length switch
    {
      "m" => eLength.m,
      "in" => eLength.inch,
      "cm" => eLength.cm,
      "mm" => eLength.mm,
      "ft" => eLength.ft,
      _ => eLength.NotApplicable
    };

    eTemperature temp = units.temperature switch
    {
      "C" => eTemperature.C,
      "F" => eTemperature.F,
      _ => eTemperature.NotApplicable
    };
    Model.SetPresentUnits_2(force, length, temp);
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
