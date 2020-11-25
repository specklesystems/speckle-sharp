using Newtonsoft.Json;

namespace Objects.Geometry
{
  public class ControlPoint : Point, IHasBoundingBox
  {
    public ControlPoint()
    {

    }

    public ControlPoint(double x, double y, double z, string units, string applicationId = null) : base(x, y, z, units, applicationId)
    {
      value.Add(1); //w = 1
    }

    public ControlPoint(double x, double y, double z, double w, string units, string applicationId = null) : base(x, y, z, units, applicationId)
    {
      value.Add(w);
    }

    [JsonIgnore]
    public double weight => value[3];

    public override string ToString() => $"{{{x},{y},{z},{weight}}}";
  }
}