using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Speckle.Core.Kits;
using Speckle.Core.Models;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections;

namespace SpeckleRhino
{
  /// <summary>
  /// Handles QA/QC for determining whether RhinoObjects can be converted to supported Speckle schemas
  /// </summary>
  public class SchemaObjectFilter
  {
    #region Properties
    public enum SupportedSchema { Floor, Wall, Roof, Column, Beam, Pipe, Duct, FaceWall, Topography, none };
    private Rhino.RhinoDoc Doc;
    public Dictionary<string, List<RhinoObject>> SchemaDictionary = new Dictionary<string, List<RhinoObject>>();
    public double minDimension = 25 * Units.GetConversionFactor(Units.Millimeters, RhinoDoc.ActiveDoc.ModelUnitSystem.ToString());
    #endregion

    #region Constructors
    public SchemaObjectFilter (List<RhinoObject> docObjects, Rhino.RhinoDoc doc, string inputSchema = null)
    {
      Doc = doc;

      // add all supported enums to schema dict
      foreach (SupportedSchema schema in Enum.GetValues(typeof(SupportedSchema)))
        SchemaDictionary.Add(schema.ToString(), new List<RhinoObject>());

      if (inputSchema == null) // no schema means automagic processing
      {
        ApplyNamingFilter(docObjects, out List<RhinoObject> unfilteredObjs);
        ApplyGeomFilter(unfilteredObjs);
      }
      else
      {
        ApplyGeomFilter(docObjects, inputSchema);
      }
    }
    #endregion

    #region Internal Methods

    // check object name and then layer path for all supported schema strings
    private void ApplyNamingFilter(List<RhinoObject> objs, out List<RhinoObject> unfilteredObjs)
    {
      // create temp filter dictionary
      var filterDictionary = new Dictionary<SupportedSchema, List<RhinoObject>>();
      foreach (SupportedSchema schema in Enum.GetValues(typeof(SupportedSchema)))
        filterDictionary.Add(schema, new List<RhinoObject>());

      unfilteredObjs = objs;
      for (int j = unfilteredObjs.Count - 1; j >= 0; j--)
      {
        RhinoObject obj = unfilteredObjs[j];
        string objName = obj.Attributes.Name;
        string objPath = Doc.Layers[obj.Attributes.LayerIndex].FullPath;

        SupportedSchema foundSchema = SupportedSchema.none;
        foreach (SupportedSchema schema in Enum.GetValues(typeof(SupportedSchema)))
        {
          if (objName != null && objName.Contains(schema.ToString()))
          { foundSchema = schema; break; }
        }
        if (foundSchema == SupportedSchema.none)
          foreach (SupportedSchema schema in Enum.GetValues(typeof(SupportedSchema)))
          {
            if (objPath.Contains(schema.ToString()))
            { foundSchema = schema; break; }
          }
        if (foundSchema != SupportedSchema.none)
        {
          // add to filter dict and remove from filter list
          filterDictionary[foundSchema].Add(obj);
          unfilteredObjs.RemoveAt(j);
        }
      }

      // process the filter dictionary and add all viable geom to the output dict
      foreach (SupportedSchema schema in filterDictionary.Keys)
        foreach (RhinoObject obj in filterDictionary[schema])
          if (IsViableSchemaObject(schema, obj))
            SchemaDictionary[schema.ToString()].Add(obj);
    }

    private void ApplyGeomFilter(List<RhinoObject> objs, string inputSchema = null)
    {
      if ( inputSchema != null)
      {
        if (Enum.TryParse<SupportedSchema>(inputSchema, out SupportedSchema schema))
          foreach (RhinoObject obj in objs)
            if (IsViableSchemaObject(schema, obj))
              SchemaDictionary[inputSchema.ToString()].Add(obj);
      }
      else
      {
        foreach (RhinoObject obj in objs)
        {
          // get viable schemas for this object
          List<SupportedSchema> schemasToTest = GetValidSchemas(obj);
          foreach (var testSchema in schemasToTest)
            if (IsViableSchemaObject(testSchema, obj))
              SchemaDictionary[testSchema.ToString()].Add(obj); break;
        }
      }
    }

    private bool IsViableSchemaObject(SupportedSchema schema, RhinoObject obj)
    {
      switch (schema)
      {
        case SupportedSchema.Column:
          try // assumes non xy linear curve > 45 deg
          {
            Curve crv = obj.Geometry as Curve;
            if (!crv.IsLinear()) break;
            double angleRad = Vector3d.VectorAngle(crv.PointAtEnd - crv.PointAtStart, Vector3d.ZAxis);
            if (angleRad > Math.PI / 2)
              angleRad = Math.PI - angleRad;
            if (angleRad < Math.PI / 4)
              return true;
          }
          catch { }
          break;
        case SupportedSchema.Beam:
          try // assumes xy linear curve of angle =< 45 deg
          {
            Curve crv = obj.Geometry as Curve;
            if (!crv.IsLinear()) break;
            double angleRad = Vector3d.VectorAngle(crv.PointAtEnd - crv.PointAtStart, Vector3d.ZAxis);
            if (angleRad > Math.PI / 2)
              angleRad = Math.PI - angleRad;
            if (angleRad >= Math.PI / 4)
              return true;
          }
          catch { }
          break;

        case SupportedSchema.Pipe:
        case SupportedSchema.Duct:
          try
          {
            Curve crv = obj.Geometry as Curve;
            if (!crv.IsClosed) return true;
          }
          catch { }
          break;
        case SupportedSchema.Floor:
        case SupportedSchema.Roof:
          try // assumes xy planar single surface
          {
            Brep brp = obj.Geometry as Brep ?? (obj.Geometry as Extrusion).ToBrep(); // assumes this has already been filtered for single surface
            if (IsPlanar(brp.Surfaces.First(), out bool singleH, out bool singleV))
              if (singleH)
                  return true;
          }
          catch { }
          break;
        case SupportedSchema.Wall:
          try // assumes z vertical planar single surface
          {
            Brep brp = obj.Geometry as Brep ?? (obj.Geometry as Extrusion).ToBrep(); // assumes this has already been filtered for single surface
            if (brp.Edges.Where(o => o.GetLength() < minDimension).Count() > 0)
              return false;
            if (IsPlanar(brp.Surfaces.First(), out bool singleH, out bool singleV))
              if (singleV)
                return true;
          }
          catch { }
          break;
        case SupportedSchema.FaceWall:
          try
          {
            Brep brp = obj.Geometry as Brep ?? (obj.Geometry as Extrusion).ToBrep(); // assumes this has already been filtered for single surface
            return true;
          }
          catch { }
          break;
        case SupportedSchema.Topography:
          try
          {
            switch (obj.Geometry)
            {
              case Mesh o:
                if (!o.IsClosed) return true;
                break;
              case Brep o:
                if (!o.IsSolid) return true;
                break;
#if RHINO7
              case SubD o:
                if (!o.IsSolid) return true;
                break;
#endif
            }
          }
          catch { }
          break;
        default:
          return false;
      }
      return false;
    }

    private bool IsPlanar(Surface srf, out bool isHorizontal, out bool isVertical)
    {
      isHorizontal = false;
      isVertical = false;

      if (srf.TryGetPlane(out Plane p))
      {
        Vector3d normal = p.Normal;
        if (normal.Unitize())
        {
          if (Math.Abs(normal.Z) == 1)
              isHorizontal = true;
          else if (normal.Z == 0)
              isVertical = true;
          return true;
        }
      }

      return false;
    }

    protected List<SupportedSchema> GetValidSchemas(RhinoObject obj)
    {
      var objSchemas = new List<SupportedSchema>();
      var type = GetCustomObjectType(obj);
      switch (type)
      {
        case ObjectType.Curve:
          Curve crv = obj.Geometry as Curve;
          if (crv.IsLinear()) // test for linearity
            objSchemas = new List<SupportedSchema> {
              SupportedSchema.Beam,
              SupportedSchema.Column }; 
          break;
        case ObjectType.Surface:
          objSchemas = new List<SupportedSchema> {
            SupportedSchema.Floor,
            SupportedSchema.Wall,
            SupportedSchema.FaceWall,
            SupportedSchema.Roof };
          break;
        case ObjectType.Mesh:
        case ObjectType.PolysrfFilter:
        case ObjectType.Brep:
        case ObjectType.Extrusion:
          objSchemas = new List<SupportedSchema>
          {
            SupportedSchema.Topography
          };
          break;
        default:
          break;
      }
      return objSchemas;
    }

    // in place just to handle brep differentiation to srfs and polysrfs
    protected ObjectType GetCustomObjectType(RhinoObject obj)
    {
      switch (obj.ObjectType)
      {
        case ObjectType.Brep:
          Brep brp = obj.Geometry as Brep;
          if (brp.Faces.Count == 1)
            return ObjectType.Surface;
          else if (!brp.IsSolid && brp.IsManifold)
            return ObjectType.PolysrfFilter;
          else return obj.ObjectType;
        case ObjectType.Extrusion:
          var ext = obj.Geometry as Extrusion;
          if (ext.IsPlanar())
            return ObjectType.Surface;
          else if (ext.HasBrepForm && !ext.IsSolid)
          {
            Brep convertedBrp = ext.ToBrep();
            if (convertedBrp.Faces.Count == 1)
              return ObjectType.Surface;
            else if (convertedBrp.IsManifold)
              return ObjectType.PolysrfFilter;
            else return obj.ObjectType;
          }
          else return obj.ObjectType;
        case ObjectType.Curve:
        case ObjectType.Surface:
        case ObjectType.Mesh:
        default:
          return obj.ObjectType;
      }
    }

    #endregion
  }

}
