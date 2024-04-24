using ArcGIS.Core.Geometry;
using Speckle.Converters.Common.Objects;
using Objects.GIS;
using Speckle.Converters.Common;
using ArcGIS.Desktop.Mapping;
using Speckle.Core.Models;
using Speckle.Converters.ArcGIS3.Geometry;

namespace Speckle.Converters.ArcGIS3.Features;

public class MultipatchFeatureToSpeckleConverter : IRawConversion<Multipatch, IReadOnlyList<Base>>
{
  private readonly IConversionContextStack<Map, Unit> _contextStack;
  private readonly IRawConversion<ReadOnlySegmentCollection, SOG.Polyline> _segmentConverter;
  private readonly IRawConversion<MapPoint, SOG.Point> _pointConverter;
  private readonly IGeometryUtils _geomUtils;

  public MultipatchFeatureToSpeckleConverter(
    IConversionContextStack<Map, Unit> contextStack,
    IRawConversion<ReadOnlySegmentCollection, SOG.Polyline> segmentConverter,
    IRawConversion<MapPoint, SOG.Point> pointConverter,
    IGeometryUtils geomUtils
  )
  {
    _contextStack = contextStack;
    _segmentConverter = segmentConverter;
    _pointConverter = pointConverter;
    _geomUtils = geomUtils;
  }

  public IReadOnlyList<Base> RawConvert(Multipatch target)
  {
    List<Base> meshList = new();
    int partCount = target.PartCount;

    // placeholder, needs to be declared in order to be used in the Ring patch type
    GisPolygonGeometry polygonGeom = new() { };

    for (int idx = 0; idx < partCount; idx++)
    {
      SOG.Mesh mesh = new() { units = _contextStack.Current.SpeckleUnits };
      int ptStartIndex = target.GetPatchStartPointIndex(idx);
      int ptCount = target.GetPatchPointCount(idx);
      List<double> pointCoords = new();

      // get the patch type to get the point arrangement in the mesh
      // https://pro.arcgis.com/en/pro-app/latest/sdk/api-reference/topic27403.html
      PatchType patchType = target.GetPatchType(idx);

      int count;
      if (patchType == PatchType.TriangleStrip)
      {
        for (int ptIdx = ptStartIndex; ptIdx < ptStartIndex + ptCount; ptIdx++)
        {
          var convertedPt = _pointConverter.RawConvert(target.Points[ptIdx]);
          pointCoords.AddRange(new List<double>() { convertedPt.x, convertedPt.y, convertedPt.z });
          count = ptIdx - ptStartIndex + 1;
          if (count > 2) // every new point adds a triangle
          {
            mesh.faces.AddRange(new List<int>() { 3, count - 3, count - 2, count - 1 });
            mesh.vertices.AddRange(pointCoords);
            pointCoords.Clear();
          }
        }
        if (_geomUtils.ValidateMesh(mesh))
        {
          meshList.Add(mesh);
        }
        else
        {
          throw new SpeckleConversionException("Multipatch part conversion did not succeed: invalid mesh generated");
        }
      }
      else if (patchType == PatchType.Triangles)
      {
        for (int ptIdx = ptStartIndex; ptIdx < ptStartIndex + ptCount; ptIdx++)
        {
          var convertedPt = _pointConverter.RawConvert(target.Points[ptIdx]);
          pointCoords.AddRange(new List<double>() { convertedPt.x, convertedPt.y, convertedPt.z });
          count = ptIdx - ptStartIndex + 1;
          if (count % 3 == 0) // every 3 new points is a new triangle
          {
            mesh.faces.AddRange(new List<int>() { 3, count - 3, count - 2, count - 1 });
            mesh.vertices.AddRange(pointCoords);
            pointCoords.Clear();
          }
        }
        if (_geomUtils.ValidateMesh(mesh))
        {
          meshList.Add(mesh);
        }
        else
        {
          throw new SpeckleConversionException("Multipatch part conversion did not succeed: invalid mesh generated");
        }
      }
      else if (patchType == PatchType.TriangleFan)
      {
        for (int ptIdx = ptStartIndex; ptIdx < ptStartIndex + ptCount; ptIdx++)
        {
          var convertedPt = _pointConverter.RawConvert(target.Points[ptIdx]);
          pointCoords.AddRange(new List<double>() { convertedPt.x, convertedPt.y, convertedPt.z });
          count = ptIdx - ptStartIndex + 1;
          if (count > 2) // every new point adds a triangle (originates from 0)
          {
            mesh.vertices.AddRange(pointCoords);
            mesh.faces.AddRange(new List<int>() { 3, 0, count - 2, count - 1 });
          }
        }
        if (_geomUtils.ValidateMesh(mesh))
        {
          meshList.Add(mesh);
        }
        else
        {
          throw new SpeckleConversionException("Multipatch part conversion did not succeed: invalid mesh generated");
        }
      }
      // in case of RingMultipatch - return GisPolygonGeometry instead of Meshes
      else if (patchType == PatchType.FirstRing)
      {
        // chech if there were already Polygons, add them to list
        if (polygonGeom.boundary != null)
        {
          meshList.Add(polygonGeom);
        }

        // first ring means a start of a new GisPolygon
        polygonGeom = new() { voids = new List<SOG.Polyline>() };

        for (int ptIdx = ptStartIndex; ptIdx < ptStartIndex + ptCount; ptIdx++)
        {
          var convertedPt = _pointConverter.RawConvert(target.Points[ptIdx]);
          pointCoords.AddRange(new List<double>() { convertedPt.x, convertedPt.y, convertedPt.z });
        }
        SOG.Polyline polyline = new(pointCoords, _contextStack.Current.SpeckleUnits) { };
        polygonGeom.boundary = polyline;

        // if it's already the last part, add to list
        if (idx == partCount - 1)
        {
          meshList.Add(polygonGeom);
        }
      }
      else if (patchType == PatchType.Ring)
      {
        for (int ptIdx = ptStartIndex; ptIdx < ptStartIndex + ptCount; ptIdx++)
        {
          var convertedPt = _pointConverter.RawConvert(target.Points[ptIdx]);
          pointCoords.AddRange(new List<double>() { convertedPt.x, convertedPt.y, convertedPt.z });
        }
        SOG.Polyline polyline = new(pointCoords, _contextStack.Current.SpeckleUnits) { };

        // every outer ring is oriented clockwise
        bool isClockwise = _geomUtils.IsClockwisePolygon(polyline);
        if (!isClockwise)
        {
          // add void to existing polygon
          polygonGeom.voids.Add(polyline);
        }
        else
        {
          // add existing polygon to list, start a new polygon
          meshList.Add(polygonGeom);
          polygonGeom = new() { voids = new List<SOG.Polyline>(), boundary = polyline };
        }
        // if it's already the last part, add to list
        if (idx == partCount - 1)
        {
          meshList.Add(polygonGeom);
        }
      }
      else
      {
        throw new NotSupportedException($"Patch type {patchType} is not supported");
      }
    }

    return meshList;
  }
}
