using System;
using System.Collections.Generic;
using Objects.Structural.Properties.Profiles;
using CSiAPIv1;
using System.Linq;
using Objects.Structural.CSI.Properties;
using Speckle.Core.Models;

namespace Objects.Converter.CSI
{
  public partial class ConverterCSI
  {
    public void SpringPropertyToNative(CSISpringProperty springProperty, ref ApplicationObject appObj)
    {
      double[] k = new double[6];
      k[0] = springProperty.stiffnessX;
      k[1] = springProperty.stiffnessY;
      k[2] = springProperty.stiffnessZ;
      k[3] = springProperty.stiffnessXX;
      k[4] = springProperty.stiffnessYY;
      k[5] = springProperty.stiffnessZZ;
      int? success = null;
      switch (springProperty.springOption)
      {
        case SpringOption.Link:
          var springOption = 1;
          success = Model.PropPointSpring.SetPointSpringProp(springProperty.name, springOption, ref k, springProperty.CYs, iGUID: springProperty.applicationId);
          break;
        case SpringOption.SoilProfileFooting:
          springOption = 2;
          appObj.Update(status: ApplicationObject.State.Skipped);
          break;
      }
      if (success == 0)
        appObj.Update(status: ApplicationObject.State.Created, createdId: $"{springProperty.name}");
      else
        appObj.Update(status: ApplicationObject.State.Failed);
    }
    public void LinearSpringPropertyToNative(CSILinearSpring linearSpringProperty, ref ApplicationObject appObj)
    {
      var linearOption1 = 0;
      var linearOption2 = 0;
      switch (linearSpringProperty.LinearOption1)
      {
        case NonLinearOptions.CompressionOnly:
          linearOption1 = 0;
          break;
        case NonLinearOptions.Linear:
          linearOption1 = 1;
          break;
        case NonLinearOptions.TensionOnly:
          linearOption1 = 2;
          break;
      }
      switch (linearSpringProperty.LinearOption2)
      {
        case NonLinearOptions.CompressionOnly:
          linearOption2 = 0;
          break;
        case NonLinearOptions.Linear:
          linearOption2 = 1;
          break;
        case NonLinearOptions.TensionOnly:
          linearOption2 = 2;
          break;
      }
      var success = Model.PropLineSpring.SetLineSpringProp(linearSpringProperty.name, linearSpringProperty.stiffnessX, linearSpringProperty.stiffnessY, linearSpringProperty.stiffnessZ, linearSpringProperty.stiffnessXX, linearOption1, linearOption2, iGUID: linearSpringProperty.applicationId);

      if (success == 0)
        appObj.Update(status: ApplicationObject.State.Created, createdId: linearSpringProperty.name);
      else
        appObj.Update(status: ApplicationObject.State.Failed);
    }
    public void AreaSpringPropertyToNative(CSIAreaSpring areaSpring, ref ApplicationObject appObj)
    {
      var linearOption1 = 0;
      switch (areaSpring.LinearOption3)
      {
        case NonLinearOptions.CompressionOnly:
          linearOption1 = 0;
          break;
        case NonLinearOptions.Linear:
          linearOption1 = 1;
          break;
        case NonLinearOptions.TensionOnly:
          linearOption1 = 2;
          break;
      }
      var success = Model.PropAreaSpring.SetAreaSpringProp(areaSpring.name, areaSpring.stiffnessX, areaSpring.stiffnessY, areaSpring.stiffnessZ, linearOption1, iGUID: areaSpring.applicationId);

      if (success == 0)
        appObj.Update(status: ApplicationObject.State.Created, createdId: areaSpring.name);
      else
        appObj.Update(status: ApplicationObject.State.Failed);
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
      Model.PropPointSpring.GetPointSpringProp(name, ref springOption, ref stiffness, ref Cys, ref soilProfile, ref footing, ref period, ref color, ref notes, ref GUID);
      switch (springOption)
      {
        case 1:
          CSISpringProperty speckleSpringProperty = new CSISpringProperty(name, Cys, stiffness[0], stiffness[1], stiffness[2], stiffness[3], stiffness[4], stiffness[5]);
          speckleSpringProperty.applicationId = GUID;
          return speckleSpringProperty;
        case 2:
          speckleSpringProperty = new CSISpringProperty(name, soilProfile, footing, period);
          speckleSpringProperty.applicationId = GUID;
          return speckleSpringProperty;
        default:
          speckleSpringProperty = new CSISpringProperty();
          return speckleSpringProperty;
      }
    }
    public CSILinearSpring LinearSpringToSpeckle(string name)
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

      var s = Model.PropLineSpring.GetLineSpringProp(name, ref stiffnessX, ref stiffnessY, ref stiffnessZ, ref stiffnessXX, ref nonLinearOpt1, ref nonLinearOpt2, ref color, ref notes, ref GUID);
      switch (nonLinearOpt1)
      {
        case 0:
          nonLinearOptions1 = NonLinearOptions.Linear;
          break;
        case 1:
          nonLinearOptions1 = NonLinearOptions.CompressionOnly;
          break;
        case 2:
          nonLinearOptions1 = NonLinearOptions.TensionOnly;
          break;
      }
      switch (nonLinearOpt2)
      {
        case 0:
          nonLinearOptions2 = NonLinearOptions.Linear;
          break;
        case 1:
          nonLinearOptions2 = NonLinearOptions.CompressionOnly;
          break;
        case 2:
          nonLinearOptions2 = NonLinearOptions.TensionOnly;
          break;
      }

      if (s == 0)
      {
        CSILinearSpring speckleLinearSpring = new CSILinearSpring(name, stiffnessX, stiffnessY, stiffnessZ, stiffnessXX, nonLinearOptions1, nonLinearOptions2, GUID);
        return speckleLinearSpring;
      }
      return null;

    }
    public CSIAreaSpring AreaSpringToSpeckle(string name)
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
      string GUID = null;
      NonLinearOptions nonLinearOptions1 = NonLinearOptions.Linear;

      var s = Model.PropAreaSpring.GetAreaSpringProp(name, ref stiffnessX, ref stiffnessY, ref stiffnessZ, ref nonLinearOpt1, ref springOption, ref soilProfile, ref endLengthRatio, ref period, ref color, ref notes, ref GUID);
      switch (nonLinearOpt1)
      {
        case 0:
          nonLinearOptions1 = NonLinearOptions.Linear;
          break;
        case 1:
          nonLinearOptions1 = NonLinearOptions.CompressionOnly;
          break;
        case 2:
          nonLinearOptions1 = NonLinearOptions.TensionOnly;
          break;
      }

      if (s == 0)
      {
        CSIAreaSpring speckleAreaSpring = new CSIAreaSpring(name, stiffnessX, stiffnessY, stiffnessZ, nonLinearOptions1, GUID);
        return speckleAreaSpring;
      }
      return null;

    }
  }
}