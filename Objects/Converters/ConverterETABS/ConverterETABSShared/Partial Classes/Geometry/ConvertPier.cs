using System;
using Objects.Structural.Geometry;
using Objects.Geometry;
using Objects.Structural.Analysis;
using System.Collections.Generic;
using Objects.Structural.ETABS.Geometry;
using Objects.Structural.ETABS.Properties;
using Speckle.Core.Models;

using ETABSv1;
using System.Linq;

namespace Objects.Converter.ETABS
{
  public partial class ConverterETABS
  {
  public void PierToNative(ETABSPier eTABSPier){
      Model.PierLabel.SetPier(eTABSPier.name);
  }
    public ETABSPier PierToSpeckle(string name)
    {
      int numberStories = 0;
      string[] storyName = null;
      int[] numAreaObjs = null;
      int[] numLineObjs = null;
      double[] axisAngle = null;
      double[] widthBot = null;
      double[] thicknessBot = null;
      double[] widthTop = null;
      double[] thicknessTop = null;
      string[] matProp = null;
      double[] centerofGravityBotX = null;
      double[] centerofGravityBotY = null;
      double[] centerofGravityBotZ = null;
      double[] centerofGravityTopX = null;
      double[] centerofGravityTopY = null;
      double[] centerofGravityTopZ = null;


      var s = Model.PierLabel.GetSectionProperties(name, ref numberStories, ref storyName, ref axisAngle, ref numAreaObjs, ref numLineObjs, ref widthBot, ref thicknessBot, ref widthTop, ref thicknessTop, ref matProp
      , ref centerofGravityBotX, ref centerofGravityBotY, ref centerofGravityBotZ, ref centerofGravityTopX, ref centerofGravityTopY, ref centerofGravityTopZ);

      var speckleETABSPier =  new ETABSPier(name,numberStories,storyName,axisAngle,numAreaObjs,numLineObjs,widthBot,thicknessBot,widthTop,thicknessTop,matProp,centerofGravityBotX,centerofGravityTopY,centerofGravityTopZ,centerofGravityTopX,centerofGravityBotY,centerofGravityBotZ);
      SpeckleModel.elements.Add(speckleETABSPier);
      return speckleETABSPier;
    }
  }
}
