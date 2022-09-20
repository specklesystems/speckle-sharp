using Objects.Primitive;
using Speckle.Core.Models;
using System.Collections.Generic;
using Objects.Other;
using Speckle.Core.Kits;

namespace Objects.Geometry
{
  //TODO: to finish
  public class Surface : Base, IHasBoundingBox, IHasArea, ITransformable<Surface>
  {
    public int degreeU { get; set; } //
    public int degreeV { get; set; } //
    public bool rational { get; set; } // 
    public double area { get; set; }
    public List<double> pointData { get; set; } //
    public int countU { get; set; } //
    public int countV { get; set; } // 
    public Box bbox { get; set; } // ignore 
    public List<double> knotsU { get; set; } //
    public List<double> knotsV { get; set; } //
    public Interval domainU { get; set; } //
    public Interval domainV { get; set; } //
    public bool closedU { get; set; } //
    public bool closedV { get; set; } //

    public string units { get; set; }

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

    public List<double> ToList()
    {
      var list = new List<double>();
      list.Add(degreeU);
      list.Add(degreeV);
      list.Add(countU);
      list.Add(countV);
      list.Add(rational ? 1 : 0);
      list.Add(closedU ? 1 : 0);
      list.Add(closedV ? 1 : 0);
      list.Add(domainU.start ?? 0); // 7
      list.Add(domainU.end ?? 1);
      list.Add(domainV.start ?? 0);
      list.Add(domainV.end ?? 1); // [0] 10

      list.Add(pointData.Count); // 11
      list.Add(knotsU.Count); // 12
      list.Add(knotsV.Count); // 13

      list.AddRange(pointData);
      list.AddRange(knotsU);
      list.AddRange(knotsV);

      list.Add(Units.GetEncodingFromUnit(units));
      list.Insert(0, list.Count);

      return list;
    }

    public static Surface FromList(List<double> list)
    {
      var srf = new Surface();
      srf.degreeU = (int)list[0];
      srf.degreeV = (int)list[1];
      srf.countU = (int)list[2];
      srf.countV = (int)list[3];
      srf.rational = list[4] == 1;
      srf.closedU = list[5] == 1;
      srf.closedV = list[6] == 1;
      srf.domainU = new Interval() { start = list[7], end = list[8] }; 
      srf.domainV = new Interval() { start = list[9], end = list[10] };

      var pointCount = (int)list[11];
      var knotsUCount = (int)list[12];
      var knotsVCount = (int)list[13];

      srf.pointData = list.GetRange(14, pointCount);
      srf.knotsU = list.GetRange(14 + pointCount, knotsUCount);
      srf.knotsV = list.GetRange(14 + pointCount + knotsUCount, knotsVCount);
      
      var u = list[list.Count - 1];
      srf.units = Units.GetUnitFromEncoding(u);
      return srf;
    }

    public bool TransformTo(Transform transform, out Surface surface)
    {
      var ptMatrix = GetControlPoints();
      foreach ( var ctrlPts in ptMatrix )
      {
        for ( int i = 0; i < ctrlPts.Count; i++ )
        {
          ctrlPts[ i ].TransformTo(transform, out var tPt);
          ctrlPts[ i ] = tPt;
        }
      }
      surface = new Surface()
      {
        degreeU = degreeU,
        degreeV = degreeV,
        countU = countU,
        countV = countV,
        rational = rational,
        closedU = closedU,
        closedV = closedV,
        domainU = domainU,
        domainV = domainV,
        knotsU = knotsU,
        knotsV = knotsV,
        units = units
      };
      surface.SetControlPoints(ptMatrix);

      return true;
    }

    public bool TransformTo(Transform transform, out ITransformable transformed)
    {
      var res = TransformTo(transform, out Surface surface);
      transformed = surface;
      return res;
    }
  }
}