﻿#if REVIT
using Autodesk.Revit.DB;
using RD = Revit.Elements; //Dynamo for Revit nodes
using Objects.Converter.Revit;

#endif
using Objects.Geometry;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Arc = Objects.Geometry.Arc;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using DS = Autodesk.DesignScript.Geometry;
using Ellipse = Objects.Geometry.Ellipse;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Spiral = Objects.Geometry.Spiral;
using Transform = Objects.Other.Transform;
using Vector = Objects.Geometry.Vector;

namespace Objects.Converter.Dynamo
{
  public partial class ConverterDynamo : ISpeckleConverter
  {

#if REVIT2023
    public static string AppName = VersionedHostApplications.DynamoRevit2023;
#elif REVIT2022
    public static string AppName = VersionedHostApplications.DynamoRevit2022;
#elif REVIT2021
    public static string AppName = VersionedHostApplications.DynamoRevit2021;
#elif REVIT
    public static string AppName = VersionedHostApplications.DynamoRevit;
#else
    public static string AppName = VersionedHostApplications.DynamoSandbox;
#endif

    public string Description => "Default Speckle Kit for Dynamo";
    public string Name => nameof(ConverterDynamo);
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";

    public ReceiveMode ReceiveMode { get; set; }

    public IEnumerable<string> GetServicedApplications() => new string[] { AppName };

    public HashSet<Exception> ConversionErrors { get; private set; } = new HashSet<Exception>();

#if REVIT
    public Document Doc { get; private set; }
#endif

    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    public ProgressReport Report => new ProgressReport();

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;

    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects) => throw new NotImplementedException();
    public void SetConverterSettings(object settings)
    {
      throw new NotImplementedException("This converter does not have any settings.");
    }

    public Base ConvertToSpeckle(object @object)
    {
      switch (@object)
      {
        case DS.Point o:
          return PointToSpeckle(o);

        case DS.Vector o:
          return VectorToSpeckle(o);

        case DS.Plane o:
          return PlaneToSpeckle(o);

        case DS.Line o:
          return LineToSpeckle(o);

        case DS.Rectangle o:
          return PolylineToSpeckle(o);

        case DS.Polygon o:
          return PolylineToSpeckle(o);

        case DS.Circle o:
          return CircleToSpeckle(o);

        case DS.Arc o:
          return ArcToSpeckle(o);

        case DS.Ellipse o:
          return EllipseToSpeckle(o);

        case DS.EllipseArc o:
          return EllipseToSpeckle(o);

        case DS.PolyCurve o:
          return PolycurveToSpeckle(o);

        case DS.NurbsCurve o:
          return CurveToSpeckle(o);

        case DS.Helix o:
          return HelixToSpeckle(o);

        case DS.Curve o: //last of the curves
          return CurveToSpeckle(o);

        case DS.Mesh o:
          return MeshToSpeckle(o);

        case DS.Cuboid o:
          return BoxToSpeckle(o);

#if REVIT
        //using the revit converter to handle Revit geometry
        case RD.Element o:
          var c = new ConverterRevit();
          c.SetContextDocument(Doc);
          return c.ConvertToSpeckle(o.InternalElement);
#endif

        default:
          throw new NotSupportedException();
      }
    }

    public object ConvertToNative(Base @object)
    {
      switch (@object)
      {
        case Point o:
          return PointToNative(o);

        case Vector o:
          return VectorToNative(o);

        case Plane o:
          return PlaneToNative(o);

        case Line o:
          return LineToNative(o);

        case Polyline o:
          return PolylineToNative(o);

        case Polycurve o:
          return PolycurveToNative(o);

        case Circle o:
          return CircleToNative(o);

        case Arc o:
          return ArcToNative(o);

        case Ellipse o:
          return EllipseToNative(o);

        case Spiral o:
          return PolylineToNative(o.displayValue);

        case Curve o:
          return CurveToNative(o);

        case Brep o:
          return BrepToNative(o);

        case Mesh o:
          return MeshToNative(o);

        case Box o:
          return BoxToNative(o);
        
        case Transform o:
          return TransformToNative(o);
        default:
          throw new NotSupportedException();
      }
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckle(x)).ToList();
    }

    public List<object> ConvertToNative(List<Base> objects)
    {
      return objects.Select(x => ConvertToNative(x)).ToList(); ;
    }

    public bool CanConvertToSpeckle(object @object)
    {
      switch (@object)
      {
        case DS.Point _:
          return true;

        case DS.Vector _:
          return true;

        case DS.Plane _:
          return true;

        case DS.Line _:
          return true;

        case DS.Rectangle _:
          return true;

        case DS.Polygon _:
          return true;

        case DS.Circle _:
          return true;

        case DS.Arc _:
          return true;

        case DS.Ellipse _:
          return true;

        case DS.EllipseArc _:
          return true;

        case DS.PolyCurve _:
          return true;

        case DS.NurbsCurve _:
          return true;

        case DS.Helix _:
          return true;

        case DS.Curve _: //last _f the curves
          return true;

        case DS.Mesh _:
          return true;

        case DS.Cuboid _:
          return true;

#if REVIT
        //using the revit converter to handle Revit geometry
        case RD.Element o:
          var c = new ConverterRevit();
          c.SetContextDocument(Doc);
          return c.CanConvertToSpeckle(o.InternalElement);
#endif

        default:
          return false;
      }
    }

    public bool CanConvertToNative(Base @object)
    {
      switch (@object)
      {
        case Point _:
        case Vector _:
        case Plane _:
        case Line _:
        case Polyline _:
        case Polycurve _:
        case Circle _:
        case Arc _:
        case Ellipse _:
        case Spiral _:
        case Curve _:
        case Brep _:
        case Mesh _:
        case Box _:
        case Transform _:
          return true;

        default:
          return false;
      }
    }


    public void SetContextDocument(object doc)
    {
#if REVIT
      Doc = (Document)doc;
#endif
    }

  }
}