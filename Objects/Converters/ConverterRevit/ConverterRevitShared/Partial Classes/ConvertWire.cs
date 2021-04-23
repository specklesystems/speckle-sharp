using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using DB = Autodesk.Revit.DB;
using Objects.BuiltElements.Revit;
using Objects.Geometry;
using Speckle.Core.Models;
using Curve = Objects.Geometry.Curve;

namespace Objects.Converter.Revit
{
  public partial class ConverterRevit
  {
    public List<ApplicationPlaceholderObject> WireToNative(BuiltElements.Wire speckleWire)
    {
      var speckleRevitWire = speckleWire as RevitWire;
      var points = PointListToNative(speckleWire.points, speckleWire.units);
      var wiringType = speckleRevitWire?.wiringType == "Arc"
        ? DB.Electrical.WiringType.Arc
        : DB.Electrical.WiringType.Chamfer;
      var wireType = GetElementType<DB.Electrical.WireType>(speckleWire);

      DB.Electrical.Wire wire = null;
      var docObj = GetExistingElementByApplicationId(speckleWire.applicationId);
      if ( docObj != null )
      {
        wire = ( DB.Electrical.Wire ) docObj;
        // if the number of vertices doesn't match, we need to create a new wire
        if ( wire.NumberOfVertices != points.Length )
          wire = null;
      }

      // update points if we can
      if ( wire != null )
        for ( var i = 0; i < wire.NumberOfVertices; i++ )
        {
          if ( points[ i ].IsAlmostEqualTo(wire.GetVertex(i)) )
            continue; // borks if we set the same point
          wire.SetVertex(i, points[ i ]);
        }

      // crete a new one if there isn't one to update
      wire ??= DB.Electrical.Wire.Create(Doc, wireType.Id, Doc.ActiveView.Id,
        wiringType,
        points, null, null);

      if ( speckleRevitWire != null )
        SetInstanceParameters(wire, speckleRevitWire);

      var placeholders = new List<ApplicationPlaceholderObject>
      {
        new ApplicationPlaceholderObject
          {applicationId = speckleWire.applicationId, ApplicationGeneratedId = wire.UniqueId, NativeObject = wire}
      };

      return placeholders;
    }

    public BuiltElements.Wire WireToSpeckle(DB.Electrical.Wire revitWire)
    {
      var speckleWire = new RevitWire();
      speckleWire.family = revitWire.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString();
      speckleWire.type = revitWire.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString();
      speckleWire.wiringType = revitWire.get_Parameter(BuiltInParameter.RBS_ELEC_WIRE_TYPE).AsValueString();
      speckleWire.level = ConvertAndCacheLevel(revitWire.ReferenceLevel.Id);

      // construction geometry for creating the wire on receive (doesn't match geometry points 🙃)
      var points = new List<double>();
      for ( var i = 0; i < revitWire.NumberOfVertices; i++ )
        points.AddRange(PointToSpeckle(revitWire.GetVertex(i)).ToList());
      speckleWire.points = points;

      // geometry
      var start = ( ( LocationCurve ) revitWire.Location ).Curve.GetEndPoint(0);
      speckleWire.segments = new List<ICurve>();
      var view = ( View ) Doc.GetElement(revitWire.OwnerViewId);
      var segmentList = revitWire.get_Geometry(new Options {View = view}).ToList();
      foreach ( var segment in segmentList )
        // transform and convert the geometry segments
        switch ( segment )
        {
          case DB.PolyLine polyLine:
            var revitLine = polyLine.GetTransformed(Transform.CreateTranslation(new XYZ(0, 0, start.Z)));
            var line = PolylineToSpeckle(revitLine);
            speckleWire.segments.Add(line);
            break;
          case DB.NurbSpline nurbSpline:
            var revitCurve = nurbSpline.CreateTransformed(
              Transform.CreateTranslation(new XYZ(0, 0, start.Z)));
            // add display value
            var curve = ( Curve ) CurveToSpeckle(revitCurve);
            var polyCoords = revitCurve.Tessellate().SelectMany(pt => PointToSpeckle(pt).ToList());
            curve.displayValue = new Polyline(polyCoords, ModelUnits);
            speckleWire.segments.Add(curve);
            break;
        }

      GetAllRevitParamsAndIds(speckleWire, revitWire, new List<string>());
      return speckleWire;
    }
  }
}