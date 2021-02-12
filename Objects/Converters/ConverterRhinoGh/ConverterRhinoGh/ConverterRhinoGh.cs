using Grasshopper.Kernel.Types;
using Objects.Geometry;
using Objects.Primitive;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Arc = Objects.Geometry.Arc;
using Box = Objects.Geometry.Box;
using Brep = Objects.Geometry.Brep;
using Circle = Objects.Geometry.Circle;
using Curve = Objects.Geometry.Curve;
using Ellipse = Objects.Geometry.Ellipse;
using Interval = Objects.Primitive.Interval;
using Line = Objects.Geometry.Line;
using Mesh = Objects.Geometry.Mesh;
using Plane = Objects.Geometry.Plane;
using Point = Objects.Geometry.Point;
using Polyline = Objects.Geometry.Polyline;

using RH = Rhino.Geometry;

using Surface = Objects.Geometry.Surface;
using Vector = Objects.Geometry.Vector;
using Objects.Other;

namespace Objects.Converter.RhinoGh
{
  public partial class ConverterRhinoGh : ISpeckleConverter
  {
    public string Description => "Default Speckle Kit for Rhino & Grasshopper";
    public string Name => nameof(ConverterRhinoGh);
    public string Author => "Speckle";
    public string WebsiteOrEmail => "https://speckle.systems";

    public IEnumerable<string> GetServicedApplications() => new string[] { Applications.Rhino, Applications.Grasshopper };

    public HashSet<Error> ConversionErrors { get; private set; } = new HashSet<Error>();

    public RhinoDoc Doc { get; private set; }

    public List<ApplicationPlaceholderObject> ContextObjects { get; set; } = new List<ApplicationPlaceholderObject>();

    public void SetContextObjects(List<ApplicationPlaceholderObject> objects) => ContextObjects = objects;

    public void SetPreviousContextObjects(List<ApplicationPlaceholderObject> objects) => throw new NotImplementedException();

    public void SetContextDocument(object doc)
    {
      Doc = (RhinoDoc)doc;
    }

    // speckle user string for custom schemas
    // TODO: address consistency weak point, since SpeckleApplySchema command in the connector needs to match this exact string!!!
    string SpeckleSchemaKey = "SpeckleSchema";

    public Base ConvertToSpeckle(object @object)
    {
      switch (@object)
      {
        case RhinoObject o:
          // Tries to convert to BuiltElements schema first
          Base conversionResult = ConvertToSpeckleBE(o.Geometry, o.Attributes.GetUserString(SpeckleSchemaKey));
          
          if (conversionResult == null)
            conversionResult = ObjectToSpeckle(o);

          conversionResult["renderMaterial"] = GetMaterial(o);

          return conversionResult;
        case Point3d o:
          return PointToSpeckle(o);

        case Rhino.Geometry.Point o:
          return PointToSpeckle(o);

        case Vector3d o:
          return VectorToSpeckle(o);

        case RH.Interval o:
          return IntervalToSpeckle(o);

        case UVInterval o:
          return Interval2dToSpeckle(o);

        case RH.Line o:
          return LineToSpeckle(o);

        case LineCurve o:
          return LineToSpeckle(o);

        case RH.Plane o:
          return PlaneToSpeckle(o);

        case Rectangle3d o:
          return PolylineToSpeckle(o);

        case RH.Circle o:
          return CircleToSpeckle(o);

        case RH.Arc o:
          return ArcToSpeckle(o);

        case ArcCurve o:
          return ArcToSpeckle(o);

        case RH.Ellipse o:
          return EllipseToSpeckle(o);

        case RH.Polyline o:
          return PolylineToSpeckle(o) as Base;
        
        case NurbsCurve o:
          return CurveToSpeckle(o) as Base;

        case PolylineCurve o:
          return PolylineToSpeckle(o);

        case PolyCurve o:
          return PolycurveToSpeckle(o);
        
        case RH.Box o:
          return BoxToSpeckle(o);

        case RH.Mesh o:
          return MeshToSpeckle(o);

        case RH.Extrusion o:
          return BrepToSpeckle(o);

        case RH.Brep o:
          return BrepToSpeckle(o.DuplicateBrep());

        case NurbsSurface o:
          return SurfaceToSpeckle(o);

        default:
          throw new NotSupportedException();
      }
    }

    public List<Base> ConvertToSpeckle(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckle(x)).ToList();
    }

    // NOTE: is there a way of retrieving class name from BuiltElements class directly? using hardcoded strings atm
    public Base ConvertToSpeckleBE(object @object, string schema = null)
    {
      if (schema == null) 
        return null;

      switch (@object)
      {
        case RhinoObject o:
          schema = o.Attributes.GetUserString(SpeckleSchemaKey);
          return ConvertToSpeckleBE(o.Geometry, schema);

        case RH.Curve o:
          switch (schema)
          {
            case "Column":
              return CurveToSpeckleColumn(o);

            case "Beam":
              return CurveToSpeckleBeam(o);

            default:
              throw new NotSupportedException();
          }

        case RH.Brep o:
          switch(schema)
          {
            case "Floor":
              return BrepToSpeckleFloor(o);

            case "Ceiling":
              return BrepToSpeckleCeiling(o);

            case "Roof":
              return BrepToSpeckleRoof(o);

            case "Wall":
              return BrepToSpeckleWall(o);

            default:
              throw new NotSupportedException();
          }

        default:
          throw new NotSupportedException();
      }
    }

    public List<Base> ConvertToSpeckleBE(List<object> objects)
    {
      return objects.Select(x => ConvertToSpeckleBE(x)).ToList();
    }

    public object ConvertToNative(Base @object)
    {
      switch (@object)
      {
        case Point o:
          return PointToNative(o);

        case Vector o:
          return VectorToNative(o);

        case Interval o:
          return IntervalToNative(o);

        case Interval2d o:
          return Interval2dToNative(o);

        case Line o:
          return LineToNative(o);

        case Plane o:
          return PlaneToNative(o);

        case Circle o:
          return CircleToNative(o);

        case Arc o:
          return ArcToNative(o);

        case Ellipse o:
          return EllipseToNative(o);

        case Polyline o:
          return PolylineToNative(o);

        case Polycurve o:
          return PolycurveToNative(o);

        case Curve o:
          return CurveToNative(o);

        case Box o:
          return BoxToNative(o);

        case Mesh o:
          return MeshToNative(o);

        case Brep o:
          return BrepToNative(o);

        case Surface o:
          return SurfaceToNative(o);

        default:
          throw new NotSupportedException();
      }
    }

    public List<object> ConvertToNative(List<Base> objects)
    {
      return objects.Select(x => ConvertToNative(x)).ToList();
    }

    public bool CanConvertToSpeckle(object @object)
    {
      switch (@object)
      {
        case Point3d _:
          return true;

        case Rhino.Geometry.Point _:
          return true;

        case Vector3d _:
          return true;

        case RH.Interval _:
          return true;

        case UVInterval _:
          return true;

        case RH.Line _:
          return true;

        case LineCurve _:
          return true;

        case RH.Plane _:
          return true;

        case Rectangle3d _:
          return true;

        case RH.Circle _:
          return true;

        case RH.Arc _:
          return true;

        case ArcCurve _:
          return true;

        case RH.Ellipse _:
          return true;

        case RH.Polyline _:
          return true;

        case PolylineCurve _:
          return true;

        case PolyCurve _:
          return true;

        case NurbsCurve _:
          return true;

        case RH.Box _:
          return true;

        case RH.Mesh _:
          return true;

        case RH.Extrusion _:
          return true;

        case RH.Brep _:
          return true;

        case NurbsSurface _:
          return true;

        default:
          return false;
      }
    }

    public bool CanConvertToNative(Base @object)
    {
      switch (@object)
      {
        case Point _:
          return true;

        case Vector _:
          return true;

        case Interval _:
          return true;

        case Interval2d _:
          return true;

        case Line _:
          return true;

        case Plane _:
          return true;

        case Circle _:
          return true;

        case Arc _:
          return true;

        case Ellipse _:
          return true;

        case Polyline _:
          return true;

        case Polycurve _:
          return true;

        case Curve _:
          return true;

        case Box _:
          return true;

        case Mesh _:
          return true;

        case Brep _:
          return true;

        case Surface _:
          return true;

        default:
          return false;
      }
    }

    private RenderMaterial GetMaterial(RhinoObject o)
    {
      var material = o.GetMaterial(true);
      var renderMaterial = new RenderMaterial();

      // If it's a default material use the display color.
      if (!material.HasId)
      {
        renderMaterial.diffuse = o.Attributes.DrawColor(Doc).ToArgb();
        return renderMaterial;
      }

      // Otherwise, extract what properties we can. 
      renderMaterial.name = material.Name;
      renderMaterial.diffuse = material.DiffuseColor.ToArgb();
      renderMaterial.emissive = material.EmissionColor.ToArgb();

      renderMaterial.opacity = 1 - material.Transparency;
      renderMaterial.metalness = material.Reflectivity;

      if (material.Name.ToLower().Contains("glass") && renderMaterial.opacity == 0) 
      {
        renderMaterial.opacity = 0.3;
      }

      return renderMaterial;
    }
  }
}