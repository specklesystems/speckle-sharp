using Newtonsoft.Json;

namespace Objects.Geometry
{
  public class ControlPoint: Point, IGeometry
  {
    public ControlPoint()
    {

    }
    
    public ControlPoint(double x, double y, double z, double w = 1, string applicationId = null) : base(x, y, z, applicationId)
    {
      value.Add(w);
    }
    
    [JsonIgnore]
    public double weight => value[3];

    public override string ToString() => $"{{{x},{y},{z},{weight}}}";
  }
}