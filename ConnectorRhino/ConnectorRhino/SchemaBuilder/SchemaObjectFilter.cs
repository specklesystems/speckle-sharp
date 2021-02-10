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
using SB = SpeckleRhino.SchemaBuilder;

namespace SpeckleRhino
{
  /// <summary>
  /// Handles QA/QC for determining whether RhinoObjects can be converted to supported Speckle schemas
  /// </summary>
  public class SchemaObjectFilter
  {

    #region Properties
    private Rhino.RhinoDoc Doc;
    public Dictionary<string, List<RhinoObject>> SchemaDictionary = new Dictionary<string, List<RhinoObject>>();

    // internal vars for processing doc objects
    private Dictionary<SB.SupportedSchema, List<RhinoObject>> filterDictionary = new Dictionary<SB.SupportedSchema, List<RhinoObject>>();
    private List<RhinoObject> objsToBeFiltered;
    #endregion

    #region Constructors
    public SchemaObjectFilter (List<RhinoObject> docObjects, Rhino.RhinoDoc doc)
    {
      Doc = doc;

      foreach (SB.SupportedSchema schema in Enum.GetValues(typeof(SB.SupportedSchema)))
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

        SB.SupportedSchema foundSchema = SB.SupportedSchema.none;
        foreach (SB.SupportedSchema schema in Enum.GetValues(typeof(SB.SupportedSchema)))
        {
          if (objName != null && objName.Contains(schema.ToString()))
          { foundSchema = schema; break; }
        }
        if (foundSchema == SB.SupportedSchema.none)
          foreach (SB.SupportedSchema schema in Enum.GetValues(typeof(SB.SupportedSchema)))
          {
            if (objPath.Contains(schema.ToString()))
            { foundSchema = schema; break; }
          }
        if (foundSchema != SB.SupportedSchema.none)
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
      foreach (SB.SupportedSchema schema in filterDictionary.Keys)
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
        if (IsViableObject(SB.SupportedSchema.Column, obj))
          SchemaDictionary[SB.SupportedSchema.Column.ToString()].Add(obj);
        else if (IsViableObject(SB.SupportedSchema.Beam, obj))
          SchemaDictionary[SB.SupportedSchema.Beam.ToString()].Add(obj);
      }
    }

    private void ProcessSurfaceObject(RhinoObject obj)
    {
      Brep brp = obj.Geometry as Brep;
      if (brp.Surfaces.Count == 1) // test as floor first and then wall if this is a single face brp
      {
        if (IsViableObject(SB.SupportedSchema.Floor, obj))
          SchemaDictionary[SB.SupportedSchema.Floor.ToString()].Add(obj);
        else if (IsViableObject(SB.SupportedSchema.Wall, obj))
          SchemaDictionary[SB.SupportedSchema.Wall.ToString()].Add(obj);
      }
      else // if multi surface, test if it may be a wall
      {
        if (IsViableObject(SB.SupportedSchema.Wall, obj))
          SchemaDictionary[SB.SupportedSchema.Wall.ToString()].Add(obj);
      }
    }

    private bool IsViableObject(SB.SupportedSchema schema, RhinoObject obj)
    {
      switch (schema)
      {
        case SB.SupportedSchema.Column:
          try // assumes non xy linear curve
          {
            Curve crv = obj.Geometry as Curve;
            if (crv.IsLinear())
              if (crv.PointAtStart.Z < crv.PointAtEnd.Z)
                return true;
          }
          catch { }
          break;
        case SB.SupportedSchema.Beam:
          try // assumes xy linear curve
          {
            Curve crv = obj.Geometry as Curve;
            if (crv.IsLinear())
              if (crv.PointAtStart.Z == crv.PointAtEnd.Z)
                return true;
          }
          catch { }
          break;
        case SB.SupportedSchema.Floor:
        case SB.SupportedSchema.Ceiling:
        case SB.SupportedSchema.Roof:
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
        case SB.SupportedSchema.Wall:
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
