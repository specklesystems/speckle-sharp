using Objects.Primitive;
using Speckle.Core.Models;
using System.Collections.Generic;
using Speckle.Core.Kits;

namespace Objects.Geometry
{
  //TODO: to finish
  public class Surface : Base, IHasBoundingBox, IHasArea
  {
    public int degreeU { get; set; }
    public int degreeV { get; set; }
    public bool rational { get; set; }
    public double area { get; set; }

    // TODO: Rewrite this to be store as a list<double>
    public List<double> pointData { get; set; }

    public int countU { get; set; }
    public int countV { get; set; }
    
    public Box bbox { get; set; }

    public List<List<ControlPoint>> GetControlPoints()
    {

      var matrix = new List<List<ControlPoint>>();
      for (var i = 0; i < countU; i++)
        matrix.Add(new List<ControlPoint>());

      for (var i = 0; i < pointData.Count; i += 4)
      {
        var uIndex = i / (countV * 4);
        matrix[uIndex]
          .Add(new ControlPoint(pointData[i], pointData[i + 1], pointData[i + 2], pointData[i + 3], units));
      }

      return matrix;
    }
    public void SetControlPoints(List<List<ControlPoint>> value)
    {
      List<double> data = new List<double>();
      countU = value.Count;
      countV = value[0].Count;
      value.ForEach(row => row.ForEach(pt =>
      {
        data.Add(pt.x);
        data.Add(pt.y);
        data.Add(pt.z);
        data.Add(pt.weight);
      }));
      pointData = data;
    }

    public List<double> knotsU { get; set; }
    public List<double> knotsV { get; set; }
    public Interval domainU { get; set; }

    public Interval domainV { get; set; }

    public bool closedU { get; set; }
    public bool closedV { get; set; }

    public Surface()
    {
      this.applicationId = null;
      this.pointData = new List<double>();
    }

    public Surface(string units = Units.Meters, string applicationId = null)
    {
      this.applicationId = applicationId;
      this.units = units;
    }

  }
}