using System;
using Objects.Structural.CSI.Properties;
using Speckle.Core.Kits;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public string SpringPropertyToNative(CSISpringProperty springProperty)
    {
      double[] k = new double[6];
      k[0] = springProperty.stiffnessX;
      k[1] = springProperty.stiffnessY;
      k[2] = springProperty.stiffnessZ;
      k[3] = springProperty.stiffnessXX;
      k[4] = springProperty.stiffnessYY;
      k[5] = springProperty.stiffnessZZ;

      switch (springProperty.springOption)
      {
        case SpringOption.Link:
          const int springOption = 1;
          int success = Model.PropPointSpring.SetPointSpringProp(
            springProperty.name,
            springOption,
            ref k,
            springProperty.CYs,
            iGUID: springProperty.applicationId
          );
          if (success != 0)
            throw new ConversionException("Failed to create or modify named point spring property");
          return springProperty.name;
        default:
          //springOption = 2;
          throw new ConversionSkippedException(
            $"Converting {nameof(SpringOption)} {springProperty.springOption} to native is not currently supported "
          );
      }
    }

    public string LinearSpringPropertyToNative(CSILinearSpring linearSpringProperty)
    {
      var linearOption1 = 0;
      var linearOption2 = 0;
      linearOption1 = linearSpringProperty.LinearOption1 switch
      {
        NonLinearOptions.CompressionOnly => 0,
        NonLinearOptions.Linear => 1,
        NonLinearOptions.TensionOnly => 2,
        _ => linearOption1
      };

      linearOption2 = linearSpringProperty.LinearOption2 switch
      {
        NonLinearOptions.CompressionOnly => 0,
        NonLinearOptions.Linear => 1,
        NonLinearOptions.TensionOnly => 2,
        _ => linearOption2
      };

      var success = Model.PropLineSpring.SetLineSpringProp(
        linearSpringProperty.name,
        linearSpringProperty.stiffnessX,
        linearSpringProperty.stiffnessY,
        linearSpringProperty.stiffnessZ,
        linearSpringProperty.stiffnessXX,
        linearOption1,
        linearOption2,
        iGUID: linearSpringProperty.applicationId
      );

      if (success != 0)
        throw new ConversionException(
          $"Failed to create/modify named line spring property {linearSpringProperty.name}"
        );

      return linearSpringProperty.name;
    }

    public string AreaSpringPropertyToNative(CSIAreaSpring areaSpring)
    {
      var linearOption1 = areaSpring.LinearOption3 switch
      {
        NonLinearOptions.CompressionOnly => 0,
        NonLinearOptions.Linear => 1,
        NonLinearOptions.TensionOnly => 2,
        _
          => throw new ArgumentOutOfRangeException(
            nameof(areaSpring),
            $"Unrecognised NonLinearOption {areaSpring.LinearOption3}"
          )
      };

      var success = Model.PropAreaSpring.SetAreaSpringProp(
        areaSpring.name,
        areaSpring.stiffnessX,
        areaSpring.stiffnessY,
        areaSpring.stiffnessZ,
        linearOption1,
        iGUID: areaSpring.applicationId
      );

      if (success != 0)
        throw new ConversionException($"Failed to create/modify named area spring property {areaSpring.name}");

      return areaSpring.name;
    }

    public CSISpringProperty SpringPropertyToSpeckle(string name)
    {
      double[] stiffness = null;
      int springOption = 0;
      string Cys = null;
      string soilProfile = null;
      string footing = null;
      double period = 0;
      int color = 0;
      string notes = null;
      string GUID = null;
      Model.PropPointSpring.GetPointSpringProp(
        name,
        ref springOption,
        ref stiffness,
        ref Cys,
        ref soilProfile,
        ref footing,
        ref period,
        ref color,
        ref notes,
        ref GUID
      );
      CSISpringProperty speckleSpringProperty;
      switch (springOption)
      {
        case 1:
          speckleSpringProperty = new(
            name,
            Cys,
            stiffness[0],
            stiffness[1],
            stiffness[2],
            stiffness[3],
            stiffness[4],
            stiffness[5]
          )
          {
            applicationId = GUID
          };
          break;
        case 2:
          speckleSpringProperty = new CSISpringProperty(name, soilProfile, footing, period) { applicationId = GUID };
          break;
        default:
          speckleSpringProperty = new CSISpringProperty();
          break;
      }
      return speckleSpringProperty;
    }

    public CSILinearSpring? LinearSpringToSpeckle(string name)
    {
      double stiffnessX = 0;
      double stiffnessY = 0;
      double stiffnessZ = 0;
      double stiffnessXX = 0;
      int nonLinearOpt1 = 0;
      int nonLinearOpt2 = 0;
      int color = 0;
      string notes = null;
      string GUID = null;
      NonLinearOptions nonLinearOptions1 = NonLinearOptions.Linear;
      NonLinearOptions nonLinearOptions2 = NonLinearOptions.Linear;

      var success = Model.PropLineSpring.GetLineSpringProp(
        name,
        ref stiffnessX,
        ref stiffnessY,
        ref stiffnessZ,
        ref stiffnessXX,
        ref nonLinearOpt1,
        ref nonLinearOpt2,
        ref color,
        ref notes,
        ref GUID
      );

      if (success != 0)
        return null;

      nonLinearOptions1 = nonLinearOpt1 switch
      {
        0 => NonLinearOptions.Linear,
        1 => NonLinearOptions.CompressionOnly,
        2 => NonLinearOptions.TensionOnly,
        _ => nonLinearOptions1
      };
      nonLinearOptions2 = nonLinearOpt2 switch
      {
        0 => NonLinearOptions.Linear,
        1 => NonLinearOptions.CompressionOnly,
        2 => NonLinearOptions.TensionOnly,
        _ => nonLinearOptions2
      };

      CSILinearSpring speckleLinearSpring =
        new(name, stiffnessX, stiffnessY, stiffnessZ, stiffnessXX, nonLinearOptions1, nonLinearOptions2, GUID);
      return speckleLinearSpring;
    }

    public CSIAreaSpring? AreaSpringToSpeckle(string name)
    {
      double stiffnessX = 0;
      double stiffnessY = 0;
      double stiffnessZ = 0;

      int nonLinearOpt1 = 0;
      int springOption = 0;
      string soilProfile = null;
      double endLengthRatio = 0;
      double period = 0;

      int color = 0;
      string notes = null;
      string guid = null;

      var success = Model.PropAreaSpring.GetAreaSpringProp(
        name,
        ref stiffnessX,
        ref stiffnessY,
        ref stiffnessZ,
        ref nonLinearOpt1,
        ref springOption,
        ref soilProfile,
        ref endLengthRatio,
        ref period,
        ref color,
        ref notes,
        ref guid
      );
      if (success != 0)
        return null;

      NonLinearOptions nonLinearOptions1 = nonLinearOpt1 switch
      {
        0 => NonLinearOptions.Linear,
        1 => NonLinearOptions.CompressionOnly,
        2 => NonLinearOptions.TensionOnly,
        _ => throw new ArgumentOutOfRangeException(null, $"Unrecognised Non linear option {nonLinearOpt1}")
      };

      CSIAreaSpring speckleAreaSpring = new(name, stiffnessX, stiffnessY, stiffnessZ, nonLinearOptions1, guid);
      return speckleAreaSpring;
    }
  }
}
