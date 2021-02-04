using System.Collections.Generic;
using Speckle.Newtonsoft.Json;

namespace Objects.Geometry
{
  public class ControlPoint : Point, IHasBoundingBox
  {
    /// <summary>
    /// OBSOLETE - This is just here for backwards compatibility.
    /// You should not use this for anything. Access coordinates using X,Y,Z and weight fields.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    private List<double> value
    {
      get { return null; }
      set
      {
        x = value[0];
        y = value[1];
        z = value[2];
        weight = value.Count > 3 ? value[3] : 1;
      }
    }
    
    public ControlPoint()
    {

    }

    public ControlPoint(double x, double y, double z, string units, string applicationId = null) : base(x, y, z, units, applicationId)
    {
      this.weight = 1;
    }

    public ControlPoint(double x, double y, double z, double w, string units, string applicationId = null) : base(x, y, z, units, applicationId)
    {
      this.weight = w;
    }

    public double weight { get; set; }

    public override string ToString() => $"{{{x},{y},{z},{weight}}}";
  }
}