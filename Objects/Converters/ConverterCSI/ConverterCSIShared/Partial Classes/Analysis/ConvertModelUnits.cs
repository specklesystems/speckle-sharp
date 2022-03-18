using System;
using System.Collections.Generic;
using System.Text;
using Objects.Structural.Analysis;
using CSiAPIv1;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public void UnitsToNative(ModelUnits units)
    {
      var force = eForce.NotApplicable;
      var length = eLength.NotApplicable;
      var temp = eTemperature.NotApplicable;

      switch (units.force)
      {
        case "N":
          force = eForce.N;
          break;
        case "kip":
          force = eForce.kip;
          break;
        case "kN":
          force = eForce.kN;
          break;
        case "lb":
          force = eForce.lb;
          break;
        case "tf":
          force = eForce.tonf;
          break;
        default:
          force = eForce.NotApplicable;
          break;
      }
      switch (units.length)
      {
        case "m":
          length = eLength.m;
          break;
        case "in":
          length = eLength.inch;
          break;
        case "cm":
          length = eLength.cm;
          break;
        case "mm":
          length = eLength.mm;
          break;
        case "ft":
          length = eLength.ft;
          break;
        default:
          length = eLength.NotApplicable;
          break;
      }
      switch (units.temperature)
      {
        case "C":
          temp = eTemperature.C;
          break;
        case "F":
          temp = eTemperature.F;
          break;
        default:
          temp = eTemperature.NotApplicable;
          break;
      }
      Model.SetPresentUnits_2(force, length, temp);
      return;
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