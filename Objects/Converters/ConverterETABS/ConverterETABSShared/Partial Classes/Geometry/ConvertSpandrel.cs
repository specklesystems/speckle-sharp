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
    public ETABSSpandrel SpandrelToSpeckle(string name)
    {
      int numberStories = 0;
      string[] storyName = null;
      int[] numAreaObjs = null;
      int[] numLineObjs = null;
      double[] length = null;
      double[] depthLeft = null;
      double[] thickLeft = null;
      double[] depthRight = null;
      double[] thickRight = null;
      string[] matProp = null;
      double[] centerofGravityLeftX = null;
      double[] centerofGravityLeftY = null;
      double[] centerofGravityLeftZ = null;
      double[] centerofGravityRightX = null;
      double[] centerofGravityRightY = null;
      double[] centerofGravityRightZ = null;

      Model.SpandrelLabel.GetSectionProperties(name, ref numberStories, ref storyName, ref numAreaObjs, ref numLineObjs, ref length, ref depthLeft,
      ref thickLeft, ref depthRight, ref thickRight, ref matProp, ref centerofGravityLeftX, ref centerofGravityLeftY, ref centerofGravityLeftZ, ref centerofGravityRightX, ref centerofGravityRightY, ref centerofGravityRightZ);

      var speckleETABSSpandrel = new ETABSSpandrel(name,numberStories,storyName,numAreaObjs,numLineObjs,length,depthLeft,thickLeft,depthRight,thickRight,matProp,centerofGravityLeftX,centerofGravityLeftY,centerofGravityLeftZ,centerofGravityRightX,centerofGravityRightY,centerofGravityRightZ);
      SpeckleModel.elements.Add(speckleETABSSpandrel);
      return speckleETABSSpandrel;
      }
  }
}
