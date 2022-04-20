using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;
using Speckle.Core.Kits;

using System.Drawing;

namespace Objects.DefaultBuildingObjectKit.Visualization
{
  public class Material : Base
  {
  public string name { get; set; }

  public double weight { get; set; }
  public string finish { get; set; } //Tekla needs this
  }

  public class RenderingMaterial: Base
  {
    //This might need to be attached as a seperate property to everything
    public string name { get; set; }
    public double opacity { get; set; } = 1;
    public double metalness { get; set; } = 0;
    public double roughness { get; set; } = 1;
    public int diffuse { get; set; } = Color.LightGray.ToArgb();
    public int emissive { get; set; } = Color.Black.ToArgb();

    public RenderingMaterial() { }

    [SchemaInfo("RenderMaterial", "Creates a render material.", "BIM", "Other")]
    public RenderingMaterial(string name, double opacity = 1, double metalness = 0, double roughness = 1, Color? diffuse = null, Color? emissive = null)
    {
      this.name = name;
      this.opacity = opacity;
      this.metalness = metalness;
      this.roughness = roughness;
      this.diffuse = (diffuse.HasValue) ? diffuse.Value.ToArgb() : Color.LightGray.ToArgb();
      this.emissive = (emissive.HasValue) ? emissive.Value.ToArgb() : Color.Black.ToArgb();
    }
  }
}
