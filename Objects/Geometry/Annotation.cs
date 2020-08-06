using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Speckle.Objects.Geometry
{
  public class Annotation : Base, IGeometry
  {
    public string text { get; set; }
    public double? textHeight { get; set; }
    public string fontName { get; set; }
    public bool? bold { get; set; }
    public bool? italic { get; set; }
    public Point location { get; set; }
    public Plane plane { get; set; }
    public Annotation()
    {

    }

    public Annotation(string text, double textHeight, string fontName, bool bold, bool italic, Plane plane, Point location, string applicationId = null)
    {
      this.text = text;
      this.textHeight = textHeight;
      this.fontName = fontName;
      this.bold = bold;
      this.italic = italic;
      this.plane = plane;
      this.location = location;
      this.applicationId = applicationId;
    }
  }
}
