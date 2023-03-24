using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace Objects.Structural.CSI.Geometry
{
  public class CSIPier : Base
  {
    public string name { get; set; }
    public int numberStories { get; set; }
    public string[] storyName { get; set; }
    public double[] axisAngle { get; set; }
    public int[] numAreaObjs { get; set; }
    public int[] numLineObjs { get; set; }
    public double[] widthBot { get; set; }
    public double[] thicknessBot { get; set; }
    public double[] widthTop { get; set; }
    public double[] thicknessTop { get; set; }
    public string[] matProp { get; set; }
    public double[] centerofGravityBotX { get; set; }
    public double[] centerofGravityBotY { get; set; }
    public double[] centerofGravityBotZ { get; set; }
    public double[] centerofGravityTopX { get; set; }
    public double[] centerofGravityTopY { get; set; }
    public double[] centerofGravityTopZ { get; set; }

    public CSIPier(string name, int numberStories, string[] storyName, double[] axisAngle, int[] numAreaObjs, int[] numLineObjs, double[] widthBot, double[] thicknessBot, double[] widthTop, double[] thicknessTop, string[] matProp, double[] centerofGravityBotX, double[] centerofGravityBotY, double[] centerofGravityBotZ, double[] centerofGravityTopX, double[] centerofGravityTopY, double[] centerofGravityTopZ)
    {
      this.name = name;
      this.numberStories = numberStories;
      this.storyName = storyName;
      this.axisAngle = axisAngle;
      this.numAreaObjs = numAreaObjs;
      this.numLineObjs = numLineObjs;
      this.widthBot = widthBot;
      this.thicknessBot = thicknessBot;
      this.widthTop = widthTop;
      this.thicknessTop = thicknessTop;
      this.matProp = matProp;
      this.centerofGravityBotX = centerofGravityBotX;
      this.centerofGravityBotY = centerofGravityBotY;
      this.centerofGravityBotZ = centerofGravityBotZ;
      this.centerofGravityTopX = centerofGravityTopX;
      this.centerofGravityTopY = centerofGravityTopY;
      this.centerofGravityTopZ = centerofGravityTopZ;
    }

    public CSIPier()
    {
    }
  }
}
