using System;
using System.Collections.Generic;
using System.Text;
using Objects.Geometry;

namespace ConverterCSIShared.Extensions
{
  internal static class LineExtensions
  {
    public static IEnumerable<Point> ToPoints(this Line line)
    {
      yield return line.start;
      yield return line.end;
    }
  }
}
