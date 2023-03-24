using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace Objects.Structural.CSI.Geometry
{
  public class CSISpandrel : Base
  {
    public string name { get; set; }
    public bool multistory { get; set; }
    public int numberStories { get; set; }
    public string[] storyName { get; set; }
    public int[] numAreaObjs { get; set; }
    public int[] numLineObjs { get; set; }
    public double[] length { get; set; }
    public double[] depthLeft { get; set; }
    public double[] thickLeft { get; set; }
    public double[] depthRight { get; set; }
    public double[] thickRight { get; set; }
    public string[] matProp { get; set; }
    public double[] centerofGravityLeftX { get; set; }
    public double[] centerofGravityLeftY { get; set; }
    public double[] centerofGravityLeftZ { get; set; }
    public double[] centerofGravityRightX { get; set; }
    public double[] centerofGravityRightY { get; set; }
    public double[] centerofGravityRightZ { get; set; }

    public CSISpandrel(string name, bool multistory, int numberStories, string[] storyName, int[] numAreaObjs, int[] numLineObjs, double[] length, double[] depthLeft, double[] thickLeft, double[] depthRight, double[] thickRight, string[] matProp, double[] centerofGravityLeftX, double[] centerofGravityLeftY, double[] centerofGravityLeftZ, double[] centerofGravityRightX, double[] centerofGravityRightY, double[] centerofGravityRightZ)
    {
      this.name = name;
      this.multistory = multistory;
      this.numberStories = numberStories;
      this.storyName = storyName;
      this.numAreaObjs = numAreaObjs;
      this.numLineObjs = numLineObjs;
      this.length = length;
      this.depthLeft = depthLeft;
      this.thickLeft = thickLeft;
      this.depthRight = depthRight;
      this.thickRight = thickRight;
      this.matProp = matProp;
      this.centerofGravityLeftX = centerofGravityLeftX;
      this.centerofGravityLeftY = centerofGravityLeftY;
      this.centerofGravityLeftZ = centerofGravityLeftZ;
      this.centerofGravityRightX = centerofGravityRightX;
      this.centerofGravityRightY = centerofGravityRightY;
      this.centerofGravityRightZ = centerofGravityRightZ;
    }

    public CSISpandrel()
    {
    }
  }
}
