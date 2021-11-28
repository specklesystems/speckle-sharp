using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace Objects.Structural.ETABS.Geometry
{
  public class ETABSSpandrel:Base
  {
    public string name { get; set; }
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

  }
}
