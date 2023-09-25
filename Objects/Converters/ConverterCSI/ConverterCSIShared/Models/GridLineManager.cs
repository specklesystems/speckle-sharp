using System;
using System.Collections.Generic;
using System.Text;
using Objects.BuiltElements;
using Objects.Geometry;

namespace ConverterCSIShared.Models
{
  internal class GridLineManager
  {
    public void Add(GridLine o)
    {
      if (o.baseLine is not Line line)
      {
        throw new ArgumentException("Non line based gridlines are not supported");
      }

      var ux = Math.Abs(line.start.x - line.end.x);
      var uy = Math.Abs(line.start.y - line.end.y);

      // get rotation from global x and y
      var r = Math.Asin(uy / ux);

      if (Math.Abs(r) < .1)
      {
        throw new Exception("TODO: support grids at an angle");
      }


    }
  }
}
