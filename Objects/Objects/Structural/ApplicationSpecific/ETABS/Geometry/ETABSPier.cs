using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;

namespace Objects.Structural.ETABS.Geometry
{
  public class ETABSPier: Base
  {
    public string name { get; set; }
    public int numberStories { get; set; }
    public string[] storyName { get; set; }
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

  }
}
