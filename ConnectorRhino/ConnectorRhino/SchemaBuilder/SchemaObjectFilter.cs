using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Speckle.Core.Kits;
using Speckle.Core.Models;
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
    enum SupportedSchema { Floor, Wall, Roof, Ceiling, Column, Beam, none };

    #region Properties
    private Rhino.RhinoDoc Doc;
    public Dictionary<string, List<RhinoObject>> SchemaDictionary = new Dictionary<string, List<RhinoObject>>();

    // internal vars for processing doc objects
    private Dictionary<SupportedSchema, List<RhinoObject>> filterDictionary = new Dictionary<SupportedSchema, List<RhinoObject>>();
    private List<RhinoObject> objsToBeFiltered;
    #endregion

    #region Constructors
    public SchemaObjectFilter (List<RhinoObject> docObjects, Rhino.RhinoDoc doc)
    {
      Doc = doc;

      foreach (SupportedSchema schema in Enum.GetValues(typeof(SupportedSchema)))
      {
        filterDictionary.Add(schema, new List<RhinoObject>());
        SchemaDictionary.Add(schema.ToString(), new List<RhinoObject>());
      }
      objsToBeFiltered = docObjects;

      ApplyNamingFilter();
      ApplyGeomFilter();
    }
    #endregion

    #region Internal Methods
    // check object name and then layer path for all supported schemas
    private void ApplyNamingFilter()
    {
      for (int j = objsToBeFiltered.Count - 1; j >= 0; j--)
      {
        RhinoObject obj = objsToBeFiltered[j];
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
          objsToBeFiltered.RemoveAt(j);
        }
        
      }
    }

    private void ApplyGeomFilter()
    {
      // process the filter dictionary first and add all viable geom to the output dict
      foreach (SupportedSchema schema in filterDictionary.Keys)
        foreach (RhinoObject obj in filterDictionary[schema])
          if (IsViableObject(schema,obj))
            SchemaDictionary[schema.ToString()].Add(obj);

      // test viability for all other brep, surface, and curve objects
      foreach (RhinoObject obj in objsToBeFiltered)
      {
        switch (obj.ObjectType)
        {
          case ObjectType.Brep:
          case ObjectType.Surface:
          case ObjectType.PolysrfFilter:
            ProcessSurfaceObject(obj);
            break;
          case ObjectType.Curve:
            ProcessCurveObject(obj);
            break;
        }
      }
    }

    private void ProcessCurveObject(RhinoObject obj)
    {
      Curve crv = obj.Geometry as Curve;
      if (crv.IsLinear()) // test for linearity
      {
        if (IsViableObject(SupportedSchema.Column, obj))
          SchemaDictionary[SupportedSchema.Column.ToString()].Add(obj);
        else if (IsViableObject(SupportedSchema.Beam, obj))
          SchemaDictionary[SupportedSchema.Beam.ToString()].Add(obj);
      }
    }

    private void ProcessSurfaceObject(RhinoObject obj)
    {
      Brep brp = obj.Geometry as Brep;
      if (brp.Surfaces.Count == 1) // test as floor first and then wall if this is a single face brp
      {
        if (IsViableObject(SupportedSchema.Floor, obj))
          SchemaDictionary[SupportedSchema.Floor.ToString()].Add(obj);
        else if (IsViableObject(SupportedSchema.Wall, obj))
          SchemaDictionary[SupportedSchema.Wall.ToString()].Add(obj);
      }
      else // if multi surface, test if it may be a wall
      {
        if (IsViableObject(SupportedSchema.Wall, obj))
          SchemaDictionary[SupportedSchema.Wall.ToString()].Add(obj);
      }
    }

    private bool IsViableObject(SupportedSchema schema, RhinoObject obj)
    {
      switch (schema)
      {
        case SupportedSchema.Column:
          try // assumes non xy linear curve
          {
            Curve crv = obj.Geometry as Curve;
            if (crv.IsLinear())
              if (crv.PointAtStart.Z < crv.PointAtEnd.Z)
                return true;
          }
          catch { }
          break;
        case SupportedSchema.Beam:
          try // assumes xy linear curve
          {
            Curve crv = obj.Geometry as Curve;
            if (crv.IsLinear())
              if (crv.PointAtStart.Z == crv.PointAtEnd.Z)
                return true;
          }
          catch { }
          break;
        case SupportedSchema.Floor:
        case SupportedSchema.Ceiling:
        case SupportedSchema.Roof:
          try // assumes xy planar single surface
          {
            Brep brp = obj.Geometry as Brep;
            if (brp.Surfaces.Count > 1) { return false; }
            if (IsPlanar(brp.Surfaces.First(), out bool singleH, out bool singleV))
              if (singleH)
                  return true;
          }
          catch { }
          break;
        case SupportedSchema.Wall:
          try // assumes z vertical planar single surface
          {
            Brep brp = obj.Geometry as Brep;
            if (brp.Surfaces.Count > 1) { return false; }
            if (IsPlanar(brp.Surfaces.First(), out bool singleH, out bool singleV))
              if (singleV)
                return true;
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
      Plane p = new Plane();

      if (srf.TryGetPlane(out p))
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
    #endregion
  }
}
